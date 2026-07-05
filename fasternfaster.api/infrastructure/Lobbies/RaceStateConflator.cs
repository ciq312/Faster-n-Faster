using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.UseCases.Interfaces.Races;

namespace FasterNFaster.Api.Infrastructure.Lobbies;

public class RaceStateConflator(IRaceBroadcaster broadcaster, ILogger<RaceStateConflator> logger)
{
    private readonly ConcurrentDictionary<Guid, LobbyBroadcast> broadcasts = new();

    public int TrackedLobbies => broadcasts.Count;

    public void Publish(Guid lobbyId, IReadOnlyList<Guid> playerIds, IReadOnlyList<ParticipantSnapshot> snapshot)
    {
        var state = broadcasts.GetOrAdd(lobbyId, _ => new LobbyBroadcast());
        Interlocked.Exchange(ref state.Latest, new RaceFrame(playerIds, snapshot));

        if (Interlocked.CompareExchange(ref state.Running, 1, 0) == 0)
            _ = Pump(lobbyId, state);
    }

    public void Prune(IReadOnlySet<Guid> activeLobbyIds)
    {
        foreach (var lobbyId in broadcasts.Keys)
            if (!activeLobbyIds.Contains(lobbyId))
                broadcasts.TryRemove(lobbyId, out _);
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
                    await broadcaster.BroadcastRaceState(frame.PlayerIds, frame.Snapshot);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Race state broadcast failed for lobby {LobbyId}", lobbyId);
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

    private sealed record RaceFrame(IReadOnlyList<Guid> PlayerIds, IReadOnlyList<ParticipantSnapshot> Snapshot);
}
