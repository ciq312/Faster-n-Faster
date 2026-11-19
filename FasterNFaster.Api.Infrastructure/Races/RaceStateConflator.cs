using System.Collections.Concurrent;
using System.Diagnostics;
using FasterNFaster.Api.Infrastructure.Shared.Telemetry;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Realtime.RaceState;
using FasterNFaster.Api.UseCases.Shared;

namespace FasterNFaster.Api.Infrastructure.Races;

public class RaceStateConflator(IBroadcaster broadcaster, ILogger<RaceStateConflator> logger)
{
    private readonly ConcurrentDictionary<Guid, LobbyBroadcast> broadcasts = new();

    public int TrackedLobbies => broadcasts.Count;

    public void Publish(Guid lobbyId, IReadOnlyList<Guid> playerIds, IReadOnlyList<RaceParticipantDTO> snapshot)
    {
        var measure = RacePerformanceMetrics.PublishDuration.Enabled || RacePerformanceMetrics.FrameAgeAtSend.Enabled;
        var published = measure ? Stopwatch.GetTimestamp() : 0;
        var state = broadcasts.GetOrAdd(lobbyId, _ => new LobbyBroadcast());
        if (Interlocked.Exchange(ref state.Latest, new RaceFrame(playerIds, snapshot, published)) is not null)
            RacePerformanceMetrics.ReplacedFrames.Add(1);

        if (Interlocked.CompareExchange(ref state.Running, 1, 0) == 0)
            _ = Pump(lobbyId, state);

        if (measure)
            RacePerformanceMetrics.PublishDuration.Record(Stopwatch.GetElapsedTime(published).TotalMilliseconds);
    }

    public void Prune(IReadOnlySet<Guid> activeLobbyIds)
    {
        foreach (var lobbyId in broadcasts.Keys)
        {
            if (!activeLobbyIds.Contains(lobbyId))
                broadcasts.TryRemove(lobbyId, out _);
        }
    }

    private async Task Pump(Guid lobbyId, LobbyBroadcast state)
    {
        do
        {
            RaceFrame? frame;
            while ((frame = Interlocked.Exchange(ref state.Latest, null)) != null)
            {
                try
                {
                    var measureBroadcast = RacePerformanceMetrics.BroadcastDuration.Enabled;
                    var started = measureBroadcast || frame.Published != 0 ? Stopwatch.GetTimestamp() : 0;
                    if (frame.Published != 0)
                        RacePerformanceMetrics.FrameAgeAtSend.Record(Stopwatch.GetElapsedTime(frame.Published, started).TotalMilliseconds);
                    await broadcaster.Broadcast(Audience.Players(frame.PlayerIds), c => c.RaceState(frame.Snapshot));
                    if (measureBroadcast)
                        RacePerformanceMetrics.BroadcastDuration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Race state broadcast failed for lobby {LobbyId}", lobbyId);
                    logger.LogFailure(SafeTelemetryLog.RaceBroadcastFailed, ex);
                }
            }

            Interlocked.Exchange(ref state.Running, 0);
        }
        while (Volatile.Read(ref state.Latest) != null && Interlocked.CompareExchange(ref state.Running, 1, 0) == 0);
    }

    private sealed class LobbyBroadcast
    {
        public RaceFrame? Latest;
        public int Running;
    }

    private sealed record RaceFrame(IReadOnlyList<Guid> PlayerIds, IReadOnlyList<RaceParticipantDTO> Snapshot, long Published);
}
