using System.Text.Json;
using FasterNFaster.LoadTest;

var phases = new Dictionary<string, DateTime>();

int users = 5;
string server = "http://localhost:8080";
bool insecure = false;
int wpm = 120;
// Where phases.json is written. Env var lets Docker point it at a mounted volume
// without changing args; --out overrides. Defaults to the app dir (local runs).
string outDir = Environment.GetEnvironmentVariable("LOADTEST_OUT") ?? AppContext.BaseDirectory;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--users": users = int.Parse(args[++i]); break;
        case "--server": server = args[++i]; break;
        case "--insecure": insecure = true; break;
        case "--wpm": wpm = int.Parse(args[++i]); break;
        case "--out": outDir = args[++i]; break;
        case "-h":
        case "--help":
            Console.WriteLine("loadtest --users N --server URL [--wpm 120] [--insecure] [--out DIR]");
            return 0;
    }
}

Console.WriteLine($"Spawning {users} bots against {server} (target {wpm} WPM)");

var bots = Enumerable.Range(0, users).Select(i => new LoadTestBot(i, server, insecure)).ToList();

// Step 1: auth + connect (batched, fail-fast — if auth breaks, nothing else matters)
const int authIntervalSeconds = 5;
const int authBatchSize = 100;
var t0 = DateTime.UtcNow;
phases["auth_start"] = t0;
for (int i = 0; i < bots.Count; i += authBatchSize)
{
    var batch = bots.Skip(i).Take(authBatchSize).ToList();
    Console.WriteLine($"[1] Auth batch {i / authBatchSize + 1}: bots {i}..{i + batch.Count - 1}");
    try
    {
        await Task.WhenAll(batch.Select(async b =>
        {
            await b.AuthenticateAsync();
            await b.ConnectHubAsync();
        }));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[!] Auth batch {i / authBatchSize + 1} failed: {ex.GetType().Name}");
        Console.WriteLine(ex.ToString());
        return 1;
    }
    Console.WriteLine($"[1] Auth batch {i / authBatchSize + 1} done, waiting {authIntervalSeconds}s before next batch");
    await Task.Delay(TimeSpan.FromSeconds(authIntervalSeconds));
}
phases["auth_end"] = DateTime.UtcNow;
Console.WriteLine($"[1] Auth+connect total: {(DateTime.UtcNow - t0).TotalMilliseconds:F0}ms");

// Step 2: host creates lobby, all bots join --------------------------------
var handleBotsLobbyJoin = async () =>
{
    LoadTestBot? host = null;
    Guid lobbyId = Guid.Empty;

    List<LoadTestBot> hosts = new();

    for (int i = 0; i < users; i++)
    {
        if (i % 10 == 0)
        {
            host = bots[i];
            lobbyId = await host.CreateLobbyAsync($"loadtest-{i / 10}");
            hosts.Add(host);
            Console.WriteLine($"[2] Lobby created: {lobbyId}");
        }
        var tJoin = DateTime.UtcNow;
        await RunSafe(bots[i], "join", x => x.JoinLobbyAsync(lobbyId));
    }

    Console.WriteLine($"All bots joined in {DateTime.UtcNow}ms");
    return hosts;
};

phases["join_start"] = DateTime.UtcNow;
var hosts = await handleBotsLobbyJoin();
phases["join_end"] = DateTime.UtcNow;

// Step 3: wait for everyone to receive the passage -------------------------
var passageWait = Task.WhenAll(bots.Select(b => b.PassageReady));
if (await Task.WhenAny(passageWait, Task.Delay(TimeSpan.FromSeconds(15))) != passageWait)
{
    var missing = bots.Count(b => b.Passage is null);
    Console.WriteLine($"[!] Passage not received by {missing}/{users} bots within 15s — aborting.");
    return 1;
}
phases["passage_ready"] = DateTime.UtcNow;
Console.WriteLine($"[4] Passage received by all bots");

// Step 4: host starts race, wait for RaceStarted ---------------------------

var startRaces = async (List<LoadTestBot> hosts) =>
{
    foreach (var host in hosts) await host.StartRaceAsync();
};

await startRaces(hosts);

var raceStartedWait = Task.WhenAll(bots.Select(b => b.RaceStarted));
if (await Task.WhenAny(raceStartedWait, Task.Delay(TimeSpan.FromSeconds(10))) != raceStartedWait)
{
    Console.WriteLine("[!] RaceStarted not received within 10s — aborting.");
    return 1;
}
phases["race_started"] = DateTime.UtcNow;
Console.WriteLine("[5] Race started — bots typing");

// Step 5: type --------------------------------------------------------------
var tRace = DateTime.UtcNow;
using var pingCts = new CancellationTokenSource();
var pingInterval = TimeSpan.FromSeconds(1);
var pingTasks = bots.Select(b => RunSafe(b, "ping", x => x.PingLoopAsync(pingInterval, pingCts.Token))).ToArray();

var typingTasks = bots.Select(b => RunSafe(b, "type", x => x.TypeRaceAsync(wpm))).ToArray();
await Task.WhenAll(typingTasks);
var raceEndedWait = Task.WhenAll(bots.Select(b => b.RaceEnded));
await Task.WhenAny(raceEndedWait, Task.Delay(TimeSpan.FromSeconds(10)));

pingCts.Cancel();
await Task.WhenAll(pingTasks);

phases["race_ended"] = DateTime.UtcNow;
var raceDuration = (DateTime.UtcNow - tRace).TotalSeconds;
Console.WriteLine($"[6] Race finished in {raceDuration:F1}s");

// Step 6: stats -------------------------------------------------------------
var allRttSamples = bots.SelectMany(b => b.PingRtts).ToArray();
var sortedMs = allRttSamples.Select(s => s.Ms).OrderBy(x => x).ToArray();
if (sortedMs.Length == 0)
{
    Console.WriteLine("No RTT samples collected.");
}
else
{
    long Pct(double p) => sortedMs[Math.Min(sortedMs.Length - 1, (int)(sortedMs.Length * p))];
    Console.WriteLine($"Ping RTT (ms over {sortedMs.Length} samples): " +
        $"p50={Pct(0.5)}  p95={Pct(0.95)}  p99={Pct(0.99)}  max={sortedMs[^1]}");
}

// Write phases + rtts to JSON for the profiler graph
var output = new
{
    phases,
    rtts = allRttSamples.Select(s => new { t = s.T, ms = s.Ms }).ToArray()
};
Directory.CreateDirectory(outDir);
var jsonPath = Path.Combine(outDir, "phases.json");
await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(output));
Console.WriteLine($"[7] Wrote {allRttSamples.Length} RTT samples + {phases.Count} phase markers to {jsonPath}");

int banned = bots.Count(b => b.WasBanned);
if (banned > 0) Console.WriteLine($"[!] {banned}/{users} bots got banned (cheater detection?)");

await Task.WhenAll(bots.Select(b => b.DisposeAsync()));
Console.WriteLine("Done.");
return 0;

static async Task RunSafe(LoadTestBot bot, string stage, Func<LoadTestBot, Task> action)
{
    try { await action(bot); }
    catch (Exception ex)
    {
        Console.WriteLine($"[bot {bot.BotId}] {stage} FAILED: {ex.GetType().Name}");
        Console.WriteLine(ex.ToString());
    }
}