using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace FasterNFaster.LoadTest;

public class LoadTestBot
{
    public int BotId { get; }
    public string Nick { get; }
    public string ServerUrl { get; }
    public bool Insecure { get; }
    public Guid? UserId { get; private set; }

    public Guid? LobbyId { get; private set; }
    public string? Passage { get; private set; }
    public bool IsConnected { get; private set; }
    public int ProgressSends => Volatile.Read(ref progressSends);
    public int LateCharacters => Volatile.Read(ref lateCharacters);
    public double MaxScheduleLatenessMs => Interlocked.Read(ref maxScheduleLatenessTicks) / (double)TimeSpan.TicksPerMillisecond;
    public IReadOnlyList<(DateTime T, double Ms)> PingRtts => pingRtts;
    public IReadOnlyCollection<ProgressLatencySample> ProgressLatencies => progressLatencies.Values.ToArray();
    public int HighestObservedIndex => highestObservedIndex;
    public JsonElement? RaceResult { get; private set; }
    public bool WasBanned { get; private set; }
    public string? BannedReason { get; private set; }
    public int PingFailures => Volatile.Read(ref pingFailures);

    public Task PassageReady => passageReadyTcs.Task;
    public Task RaceStarted => raceStartedTcs.Task;
    public Task RaceEnded => raceEndedTcs.Task;

    private readonly CookieContainer cookies = new();
    private readonly List<(DateTime T, double Ms)> pingRtts = new();
    private readonly ConcurrentDictionary<int, ProgressLatencySample> progressLatencies = new();
    private readonly TaskCompletionSource passageReadyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource raceStartedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource raceEndedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private HttpClient? http;
    private HubConnection? hub;
    private int highestObservedIndex = -1;
    private int pingFailures;
    private int progressSends;
    private int lateCharacters;
    private long maxScheduleLatenessTicks;

    public LoadTestBot(int botId, string serverUrl, bool insecure)
    {
        BotId = botId;
        Nick = $"bot_{botId:D4}";
        ServerUrl = serverUrl.TrimEnd('/');
        Insecure = insecure;
    }

    public async Task AuthenticateAsync()
    {
        var handler = new HttpClientHandler { CookieContainer = cookies, UseCookies = true };
        if (Insecure) handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        http = new HttpClient(handler) { BaseAddress = new Uri(ServerUrl) };

        var resp = await http.PostAsJsonAsync("/api/auth/guest", new { Nick });
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Bot {BotId} auth failed: {(int)resp.StatusCode} {resp.ReasonPhrase} — {body}");
        }

        var bodyJson = await resp.Content.ReadFromJsonAsync<JsonElement>();
        UserId = Guid.Parse(GetCaseInsensitive(bodyJson, "guestId").GetString()!);
    }

    public async Task ConnectHubAsync()
    {
        var cookieHeader = cookies.GetCookieHeader(new Uri(ServerUrl));
        if (string.IsNullOrEmpty(cookieHeader))
            throw new InvalidOperationException($"Bot {BotId}: no cookies set ");

        hub = new HubConnectionBuilder()
            .WithUrl($"{ServerUrl}/gameHub", options =>
            {
                options.Headers["Cookie"] = cookieHeader;
                if (Insecure)
                {
                    options.HttpMessageHandlerFactory = h =>
                    {
                        if (h is HttpClientHandler hc)
                            hc.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                        return h;
                    };
                }
            })
            .WithAutomaticReconnect()
            .Build();

        hub.On<JsonElement>("LobbyState", state =>
        {
            if (Passage != null) return;
            if (TryGetPassage(state, out var passage))
            {
                Passage = passage;
                passageReadyTcs.TrySetResult();
            }
        });

        hub.On("RaceStarted", () => raceStartedTcs.TrySetResult());
        hub.On<JsonElement>("RaceState", state => ObserveRaceState(state));
        hub.On<JsonElement>("RaceEnded", result =>
        {
            RaceResult = result;
            if (ContainsOwnResult(result))
            {
                var finishIndex = (Passage?.Length ?? 0) - 1;
                if (progressLatencies.TryGetValue(finishIndex, out var finalSample))
                    finalSample.TryObserve(Stopwatch.GetTimestamp());
            }
            raceEndedTcs.TrySetResult();
        });
        hub.On<string>("Banned", reason =>
        {
            WasBanned = true;
            BannedReason = reason;
            passageReadyTcs.TrySetException(new InvalidOperationException($"banned: {reason}"));
            raceStartedTcs.TrySetException(new InvalidOperationException($"banned: {reason}"));
            raceEndedTcs.TrySetException(new InvalidOperationException($"banned: {reason}"));
        });
        hub.On("AnotherSessionStarted", () =>
            Console.WriteLine($"[bot {BotId}] another session started — duplicate auth?"));

        await hub.StartAsync();
        IsConnected = true;
    }

    public async Task<Guid> CreateLobbyAsync(string lobbyName, bool isPrivate = false)
    {
        if (http is null) throw new InvalidOperationException("Authenticate first.");
        var resp = await http.PostAsJsonAsync("/api/lobbies", new { LobbyName = lobbyName, IsPrivate = isPrivate });
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Bot {BotId} create lobby failed: {(int)resp.StatusCode} — {body}");
        }
        var result = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var lobbyId = Guid.Parse(GetCaseInsensitive(result, "lobbyId").GetString()!);
        LobbyId = lobbyId;
        return lobbyId;
    }

    public async Task JoinLobbyAsync(Guid lobbyId, string? inviteCode = null)
    {
        if (hub is null) throw new InvalidOperationException("Hub not connected.");
        LobbyId = lobbyId;
        await hub.InvokeAsync("ConnectToLobby", lobbyId, inviteCode);
    }

    public async Task StartRaceAsync()
    {
        if (hub is null) throw new InvalidOperationException("Hub not connected.");
        await hub.InvokeAsync("StartRace");
    }

    public async Task TypeRaceAsync(int targetWpm = 120, CancellationToken ct = default)
    {
        if (hub is null) throw new InvalidOperationException("Hub not connected.");
        var passage = Passage ?? throw new InvalidOperationException("Passage not received.");

        // 1 WPM = 5 chars/min. delayMs = 60000 / charsPerSec = 60000 / (wpm*5/60) = 12000/wpm
        if (targetWpm <= 0) throw new ArgumentOutOfRangeException(nameof(targetWpm));
        var baseDelayMs = Math.Max(10, 12000 / targetWpm);
        var jitter = new Random(BotId);
        var sw = Stopwatch.StartNew();
        var sendInterval = TimeSpan.FromMilliseconds(100);
        var nextSend = TimeSpan.Zero;
        var plannedTypingAt = TimeSpan.Zero;
        var pending = new List<Keystroke>();
        var milestones = Enumerable.Range(0, 5)
            .Select(i => (int)Math.Ceiling((passage.Length - 1) * i / 4d))
            .Distinct()
            .ToArray();

        for (var index = 0; index < passage.Length; index++)
        {
            if (WasBanned) return;
            ct.ThrowIfCancellationRequested();
            if (index > 0)
            {
                var jitterSpread = Math.Max(1, baseDelayMs / 5);
                plannedTypingAt += TimeSpan.FromMilliseconds(baseDelayMs + jitter.Next(-jitterSpread, jitterSpread));
                var remaining = plannedTypingAt - sw.Elapsed;
                if (remaining > TimeSpan.Zero) await Task.Delay(remaining, ct);

                var lateness = sw.Elapsed - plannedTypingAt;
                if (lateness > TimeSpan.FromMilliseconds(10)) Interlocked.Increment(ref lateCharacters);
                UpdateMax(ref maxScheduleLatenessTicks, lateness.Ticks);
            }
            pending.Add(new Keystroke(passage[index].ToString()));

            var isFinal = index == passage.Length - 1;
            var sendStartedAt = sw.Elapsed;
            if (index == 0 || isFinal || sendStartedAt >= nextSend)
            {
                foreach (var milestone in milestones.Where(m => m <= index))
                    progressLatencies.TryAdd(milestone, new ProgressLatencySample(milestone, Stopwatch.GetTimestamp()));

                await hub.SendCoreAsync("UpdateRaceState", [pending.ToArray()], ct);
                pending.Clear();
                Interlocked.Increment(ref progressSends);
                nextSend = sendStartedAt + sendInterval;
            }
        }
    }

    private sealed record Keystroke(string Key);

    private static void UpdateMax(ref long max, long candidate)
    {
        var current = Interlocked.Read(ref max);
        while (candidate > current)
        {
            var observed = Interlocked.CompareExchange(ref max, candidate, current);
            if (observed == current) return;
            current = observed;
        }
    }

    private void ObserveRaceState(JsonElement state)
    {
        if (UserId is null || state.ValueKind != JsonValueKind.Array) return;
        foreach (var participant in state.EnumerateArray())
        {
            if (!TryGetProp(participant, "playerId", out var idElement) ||
                !Guid.TryParse(idElement.GetString(), out var id) || id != UserId ||
                !TryGetProp(participant, "index", out var indexElement) ||
                !indexElement.TryGetInt32(out var index))
            {
                continue;
            }

            Interlocked.Exchange(ref highestObservedIndex, Math.Max(HighestObservedIndex, index));
            foreach (var sample in progressLatencies.Values.Where(s => s.Index <= index))
                sample.TryObserve(Stopwatch.GetTimestamp());
            return;
        }
    }

    private bool ContainsOwnResult(JsonElement result)
    {
        if (UserId is null || !TryGetProp(result, "results", out var results) || results.ValueKind != JsonValueKind.Array)
            return false;
        return results.EnumerateArray().Any(item =>
            TryGetProp(item, "playerId", out var playerId) &&
            Guid.TryParse(playerId.GetString(), out var id) && id == UserId);
    }

    public async Task PingLoopAsync(TimeSpan interval, CancellationToken ct)
    {
        if (hub is null) throw new InvalidOperationException("Hub not connected.");

        var sw = new System.Diagnostics.Stopwatch();
        while (!ct.IsCancellationRequested)
        {
            try
            {
                sw.Restart();
                await hub.InvokeAsync<long>("Ping", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), cancellationToken: ct);
                sw.Stop();
                pingRtts.Add((DateTime.UtcNow, sw.Elapsed.TotalMilliseconds));
            }
            catch (OperationCanceledException) { return; }
            catch { Interlocked.Increment(ref pingFailures); }

            try { await Task.Delay(interval, ct); }
            catch (OperationCanceledException) { return; }
        }
    }

    private static bool TryGetPassage(JsonElement state, out string passage)
    {
        passage = "";
        if (!TryGetProp(state, "settings", out var settings)) return false;
        if (!TryGetProp(settings, "passage", out var passageEl)) return false;
        if (passageEl.ValueKind != JsonValueKind.String) return false;
        passage = passageEl.GetString() ?? "";
        return passage.Length > 0;
    }

    private static bool TryGetProp(JsonElement el, string name, out JsonElement value)
    {
        if (el.ValueKind != JsonValueKind.Object) { value = default; return false; }
        foreach (var prop in el.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static JsonElement GetCaseInsensitive(JsonElement el, string name)
    {
        if (TryGetProp(el, name, out var value)) return value;
        throw new InvalidOperationException($"Property '{name}' not found in response.");
    }

    public async Task<double> PingAsync()
    {
        if (hub is null) throw new InvalidOperationException("Hub not connected.");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await hub.InvokeAsync<long>("Ping", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    public async Task DisposeAsync()
    {
        if (hub is not null) await hub.DisposeAsync();
        http?.Dispose();
    }
}

public sealed class ProgressLatencySample(int index, long sentTimestamp)
{
    private long observedTimestamp;

    public int Index { get; } = index;
    public double? LatencyMs => observedTimestamp == 0
        ? null
        : (observedTimestamp - sentTimestamp) * 1000d / Stopwatch.Frequency;

    public void TryObserve(long timestamp) => Interlocked.CompareExchange(ref observedTimestamp, timestamp, 0);
}
