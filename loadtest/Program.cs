using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FasterNFaster.LoadTest;

var phases = new Dictionary<string, DateTime>();
var phaseOffsetsMs = new Dictionary<string, double>();
var runClock = Stopwatch.StartNew();
var errors = new List<string>();
var runStarted = DateTime.UtcNow;
var runId = $"{runStarted:yyyyMMdd'T'HHmmssfff'Z'}-{Guid.NewGuid():N}"[..32];
var users = 5;
var server = "http://localhost:8080";
var insecure = false;
var wpm = 120;
var allowRemote = false;
var outDir = Environment.GetEnvironmentVariable("LOADTEST_OUT") ?? AppContext.BaseDirectory;
var bots = new List<LoadTestBot>();

try
{
    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--users": users = int.Parse(NextArg(args, ref i)); break;
            case "--server": server = NextArg(args, ref i); break;
            case "--insecure": insecure = true; break;
            case "--allow-remote": allowRemote = true; break;
            case "--wpm": wpm = int.Parse(NextArg(args, ref i)); break;
            case "--out": outDir = NextArg(args, ref i); break;
            case "-h":
            case "--help":
                Console.WriteLine("loadtest --users N --server URL [--wpm 120] [--insecure] [--allow-remote] [--out DIR]");
                return 0;
            default: throw new ArgumentException($"Unknown argument: {args[i]}");
        }
    }

    if (users < 1) throw new ArgumentOutOfRangeException(nameof(users), "At least one bot is required.");
    if (wpm < 1) throw new ArgumentOutOfRangeException(nameof(wpm), "WPM must be positive.");
    if (!Uri.TryCreate(server, UriKind.Absolute, out var serverUri) || serverUri.Scheme is not ("http" or "https"))
        throw new ArgumentException("--server must be an absolute HTTP(S) URL.");
    if (!allowRemote && !IsLocalTarget(serverUri.Host))
        throw new ArgumentException("Remote targets are blocked by default. Use --allow-remote only when an external load test is explicitly intended.");

    Console.WriteLine($"Spawning {users} bots against {server} (target {wpm} WPM)");
    MarkPhase("preflight_start");
    await WaitForHttpAsync(serverUri, TimeSpan.FromSeconds(30));
    MarkPhase("preflight_end");
    bots.AddRange(Enumerable.Range(0, users).Select(i => new LoadTestBot(i, server, insecure)));
    var raceEnded = false;
    try
    {
        const int authBatchSize = 100;
        var semaphore = new SemaphoreSlim(10);
        MarkPhase("auth_start");
        for (var i = 0; i < bots.Count; i += authBatchSize)
        {
            var batch = bots.Skip(i).Take(authBatchSize).ToList();
            Console.WriteLine($"[1] Auth batch {i / authBatchSize + 1}: bots {i}..{i + batch.Count - 1}");
            await Task.WhenAll(batch.Select(async bot =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await Task.Delay(Random.Shared.Next(100, 1000));
                    await bot.AuthenticateAsync();
                    await bot.ConnectHubAsync();
                }
                finally { semaphore.Release(); }
            }));
        }
        MarkPhase("auth_end");

        var hosts = new List<LoadTestBot>();
        MarkPhase("join_start");
        for (var i = 0; i < users; i++)
        {
            if (i % 10 == 0)
            {
                var host = bots[i];
                var lobbyId = await host.CreateLobbyAsync($"loadtest-{i / 10}");
                hosts.Add(host);
                Console.WriteLine($"[2] Lobby created: {lobbyId}");
                // Creation adds the host directly; the idempotent hub join publishes the first lobby snapshot.
                await host.JoinLobbyAsync(lobbyId);
            }
            else
            {
                await bots[i].JoinLobbyAsync(hosts[^1].LobbyId!.Value);
            }
        }
        MarkPhase("join_end");

        MarkPhase("passage_wait_start");
        await AwaitAll(bots.Select(bot => bot.PassageReady), TimeSpan.FromSeconds(15), "passage delivery");
        MarkPhase("passage_ready");

        await Task.WhenAll(hosts.Select(host => host.StartRaceAsync()));
        MarkPhase("race_start_wait");
        await AwaitAll(bots.Select(bot => bot.RaceStarted), TimeSpan.FromSeconds(10), "race start event");
        MarkPhase("race_started");

        using var runCts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var pingTasks = bots.Select(bot => bot.PingLoopAsync(TimeSpan.FromSeconds(1), runCts.Token)).ToArray();
        MarkPhase("typing_start");
        await Task.WhenAll(bots.Select(bot => bot.TypeRaceAsync(wpm, runCts.Token)));
        await AwaitAll(bots.Select(bot => bot.RaceEnded), TimeSpan.FromSeconds(15), "race end event");
        raceEnded = true;
        MarkPhase("race_ended");
        runCts.Cancel();
        await Task.WhenAll(pingTasks);

        foreach (var bot in bots)
        {
            if (bot.WasBanned) errors.Add($"Bot {bot.BotId} was banned: {bot.BannedReason}");
            if (bot.HighestObservedIndex < 0)
                errors.Add($"Bot {bot.BotId} received no server-confirmed RaceState progress.");
            if (!ContainsPlayerResult(bot.RaceResult, bot.UserId))
                errors.Add($"Bot {bot.BotId} is missing from the RaceEnded result payload.");
            if (bot.PingFailures > 0) errors.Add($"Bot {bot.BotId} had {bot.PingFailures} Ping invocation failure(s).");
        }

        var pingSamples = bots.SelectMany(bot => bot.PingRtts.Select(sample => new
        {
            botId = bot.BotId,
            t = sample.T,
            ms = sample.Ms
        })).ToArray();
        var progressSamples = bots.SelectMany(bot => bot.ProgressLatencies.Select(sample => new
        {
            botId = bot.BotId,
            index = sample.Index,
            latencyMs = sample.LatencyMs
        })).ToArray();

        var sortedPing = pingSamples.Select(sample => sample.ms).OrderBy(value => value).ToArray();
        double? Percentile(double p) => sortedPing.Length == 0
            ? null
            : sortedPing[Math.Min(sortedPing.Length - 1, (int)Math.Ceiling(sortedPing.Length * p) - 1)];
        var sortedProgress = progressSamples.Where(sample => sample.latencyMs.HasValue)
            .Select(sample => sample.latencyMs!.Value).OrderBy(value => value).ToArray();
        double? ProgressPercentile(double p) => sortedProgress.Length == 0
            ? null
            : sortedProgress[Math.Min(sortedProgress.Length - 1, (int)Math.Ceiling(sortedProgress.Length * p) - 1)];

        var summary = new
        {
            schemaVersion = 2,
            runId,
            startedAtUtc = runStarted,
            finishedAtUtc = DateTime.UtcNow,
            target = new { server, users, wpm, rateLimitingExpectedDisabled = true, remoteTargetAllowed = allowRemote },
            outcome = errors.Count == 0 && raceEnded ? "passed" : "failed",
            reasons = errors,
            requestedBots = users,
            authenticatedBots = bots.Count(bot => bot.UserId.HasValue),
            connectedBots = bots.Count(bot => bot.IsConnected),
            progressSends = bots.Sum(bot => bot.ProgressSends),
            plannedTypingCharacters = bots.Sum(bot => bot.Passage?.Length ?? 0),
            lateTypingCharacters = bots.Sum(bot => bot.LateCharacters),
            maxTypingScheduleLatenessMs = bots.Count == 0 ? (double?)null : bots.Max(bot => bot.MaxScheduleLatenessMs),
            completedLobbyRaces = raceEnded ? hosts.Count : 0,
            raceResultsReceived = bots.Count(bot => bot.RaceResult.HasValue),
            pingFailures = bots.Sum(bot => bot.PingFailures),
            pingRttMs = new { count = sortedPing.Length, p50 = Percentile(.50), p95 = Percentile(.95), p99 = Percentile(.99), max = sortedPing.Length == 0 ? (double?)null : sortedPing[^1] },
            progressLatencyMs = new
            {
                count = progressSamples.Count(sample => sample.latencyMs.HasValue),
                timedOut = progressSamples.Count(sample => !sample.latencyMs.HasValue),
                p50 = ProgressPercentile(.50),
                p95 = ProgressPercentile(.95),
                p99 = ProgressPercentile(.99),
                max = sortedProgress.Length == 0 ? (double?)null : sortedProgress[^1],
                samples = progressSamples
            },
            phases,
            phaseOffsetsMs,
            pingRtts = pingSamples,
            bots = bots.Select(bot => new
            {
                bot.BotId,
                bot.UserId,
                bot.IsConnected,
                bot.ProgressSends,
                bot.LateCharacters,
                bot.MaxScheduleLatenessMs,
                bot.HighestObservedIndex,
                passageLength = bot.Passage?.Length ?? 0,
                resultReceived = bot.RaceResult.HasValue,
                bot.WasBanned,
                bot.BannedReason
            })
        };

        Directory.CreateDirectory(outDir);
        var artifactDir = Path.Combine(outDir, runId);
        Directory.CreateDirectory(artifactDir);
        var runPath = Path.Combine(artifactDir, "run-summary.json");
        await File.WriteAllTextAsync(runPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        var phasesPath = Path.Combine(outDir, "phases.json");
        await File.WriteAllTextAsync(phasesPath, JsonSerializer.Serialize(new { phases, rtts = pingSamples }));
        await File.WriteAllTextAsync(Path.Combine(artifactDir, "phases.json"), JsonSerializer.Serialize(new { phases, rtts = pingSamples }));
        Console.WriteLine($"[7] Wrote run summary to {runPath}");
        Console.WriteLine($"Ping RTT (ms over {sortedPing.Length} samples): p50={Percentile(.50)?.ToString() ?? "n/a"} p95={Percentile(.95)?.ToString() ?? "n/a"} p99={Percentile(.99)?.ToString() ?? "n/a"}");
    }
    finally
    {
        await Task.WhenAll(bots.Select(bot => bot.DisposeAsync()));
    }

    if (errors.Count > 0)
    {
        foreach (var error in errors) Console.Error.WriteLine($"[!] {error}");
        return 1;
    }
    Console.WriteLine("Load test completed with validated race results.");
    return 0;
}
catch (Exception exception)
{
    errors.Add($"{exception.GetType().Name}: {exception.Message}");
    Console.Error.WriteLine($"[!] Load test failed: {exception}");
    try
    {
        Directory.CreateDirectory(outDir);
        var artifactDir = Path.Combine(outDir, runId);
        Directory.CreateDirectory(artifactDir);
        var partialPings = bots.SelectMany(bot => bot.PingRtts.Select(sample => new { botId = bot.BotId, t = sample.T, ms = sample.Ms })).ToArray();
        var partialProgress = bots.SelectMany(bot => bot.ProgressLatencies.Select(sample => new { botId = bot.BotId, index = sample.Index, latencyMs = sample.LatencyMs })).ToArray();
        var failureSummary = new
        {
            schemaVersion = 2,
            runId,
            startedAtUtc = runStarted,
            finishedAtUtc = DateTime.UtcNow,
            target = new { server, users, wpm, remoteTargetAllowed = allowRemote },
            outcome = "failed",
            reasons = errors,
            requestedBots = users,
            authenticatedBots = bots.Count(bot => bot.UserId.HasValue),
            connectedBots = bots.Count(bot => bot.IsConnected),
            progressSends = bots.Sum(bot => bot.ProgressSends),
            completedLobbyRaces = bots.Where(bot => bot.RaceResult.HasValue)
                .Select(bot => bot.LobbyId).Where(id => id.HasValue).Distinct().Count(),
            plannedTypingCharacters = bots.Sum(bot => bot.Passage?.Length ?? 0),
            lateTypingCharacters = bots.Sum(bot => bot.LateCharacters),
            maxTypingScheduleLatenessMs = bots.Count == 0 ? (double?)null : bots.Max(bot => bot.MaxScheduleLatenessMs),
            raceResultsReceived = bots.Count(bot => bot.RaceResult.HasValue),
            pingFailures = bots.Sum(bot => bot.PingFailures),
            pingRtts = partialPings,
            progressLatencySamples = partialProgress,
            bots = bots.Select(bot => new { bot.BotId, bot.UserId, bot.IsConnected, bot.ProgressSends, bot.LateCharacters, bot.MaxScheduleLatenessMs, bot.HighestObservedIndex, bot.WasBanned, bot.BannedReason }),
            phases,
            phaseOffsetsMs,
        };
        await File.WriteAllTextAsync(Path.Combine(artifactDir, "run-summary.json"),
            JsonSerializer.Serialize(failureSummary, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(outDir, "phases.json"), JsonSerializer.Serialize(new { phases, rtts = partialPings }));
        await File.WriteAllTextAsync(Path.Combine(artifactDir, "phases.json"), JsonSerializer.Serialize(new { phases, rtts = partialPings }));
    }
    catch (Exception artifactError)
    {
        Console.Error.WriteLine($"[!] Could not write failure artifacts: {artifactError.Message}");
    }
    return 1;
}

static string NextArg(string[] allArgs, ref int index)
{
    if (++index >= allArgs.Length) throw new ArgumentException($"Missing value after {allArgs[index - 1]}.");
    return allArgs[index];
}

void MarkPhase(string name)
{
    phases[name] = DateTime.UtcNow;
    phaseOffsetsMs[name] = runClock.Elapsed.TotalMilliseconds;
}

static async Task AwaitAll(IEnumerable<Task> tasks, TimeSpan timeout, string operation)
{
    var all = Task.WhenAll(tasks);
    if (await Task.WhenAny(all, Task.Delay(timeout)) != all)
        throw new TimeoutException($"Timed out waiting for {operation}.");
    await all;
}

static async Task WaitForHttpAsync(Uri server, TimeSpan timeout)
{
    using var client = new HttpClient { BaseAddress = server, Timeout = TimeSpan.FromSeconds(2) };
    var timer = Stopwatch.StartNew();
    Exception? lastError = null;
    while (timer.Elapsed < timeout)
    {
        try
        {
            using var response = await client.GetAsync("/");
            if ((int)response.StatusCode < 500) return;
            lastError = new HttpRequestException($"API readiness returned {(int)response.StatusCode}.");
        }
        catch (Exception exception) { lastError = exception; }
        await Task.Delay(TimeSpan.FromMilliseconds(500));
    }
    throw new TimeoutException($"API did not become ready at {server} within {timeout}.", lastError);
}

static bool ContainsPlayerResult(JsonElement? result, Guid? userId)
{
    if (result is not JsonElement payload || payload.ValueKind != JsonValueKind.Object || userId is null) return false;
    if (!TryGetProp(payload, "results", out var results) || results.ValueKind != JsonValueKind.Array) return false;
    return results.EnumerateArray().Any(item => TryGetProp(item, "playerId", out var id) &&
        Guid.TryParse(id.GetString(), out var parsed) && parsed == userId);
}

static bool TryGetProp(JsonElement element, string name, out JsonElement value)
{
    if (element.ValueKind == JsonValueKind.Object)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }
    }
    value = default;
    return false;
}

static bool IsLocalTarget(string host) => host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
    IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address) ||
    host.Equals("api", StringComparison.OrdinalIgnoreCase);
