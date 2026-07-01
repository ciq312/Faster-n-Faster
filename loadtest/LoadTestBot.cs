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

    public Guid? LobbyId { get; private set; }
    public string? Passage { get; private set; }
    public IReadOnlyList<(DateTime T, long Ms)> PingRtts => pingRtts;
    public bool WasBanned { get; private set; }
    public string? BannedReason { get; private set; }

    public Task PassageReady => passageReadyTcs.Task;
    public Task RaceStarted => raceStartedTcs.Task;
    public Task RaceEnded => raceEndedTcs.Task;

    private readonly CookieContainer cookies = new();
    private readonly List<(DateTime T, long Ms)> pingRtts = new();
    private readonly TaskCompletionSource passageReadyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource raceStartedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource raceEndedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private HttpClient? http;
    private HubConnection? hub;

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
        hub.On<JsonElement>("RaceEnded", _ => raceEndedTcs.TrySetResult());
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
        if (Passage is null) throw new InvalidOperationException("Passage not received.");

        // 1 WPM = 5 chars/min. delayMs = 60000 / charsPerSec = 60000 / (wpm*5/60) = 12000/wpm
        int baseDelayMs = Math.Max(10, 12000 / targetWpm);
        var jitter = new Random(BotId);
        var sw = new System.Diagnostics.Stopwatch();

        for (int index = 0; index < Passage.Length; index++)
        {
            if (WasBanned) return;
            ct.ThrowIfCancellationRequested();
            var typedPrefix = Passage[..(index + 1)];

            // fire-and-forget: matches what the real client should do; latency is measured separately via PingLoopAsync
            await hub.SendAsync("UpdateRaceState", index, 0, typedPrefix, cancellationToken: ct);

            // ±20% jitter so bots don't keystroke in lockstep
            int jitterSpread = Math.Max(1, baseDelayMs / 5);
            int delay = baseDelayMs + jitter.Next(-jitterSpread, jitterSpread);
            if (delay > 0) await Task.Delay(delay, ct);
        }
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
                pingRtts.Add((DateTime.UtcNow, sw.ElapsedMilliseconds));
            }
            catch (OperationCanceledException) { return; }
            catch { /* ignore transient hub errors during shutdown */ }

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

    public async Task<long> PingAsync()
    {
        if (hub is null) throw new InvalidOperationException("Hub not connected.");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await hub.InvokeAsync<long>("Ping", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    public async Task DisposeAsync()
    {
        if (hub is not null) await hub.DisposeAsync();
        http?.Dispose();
    }
}
