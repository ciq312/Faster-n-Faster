using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Races.Ports;

namespace FasterNFaster.Api.Infrastructure.Races;

public class RaceTickRegistry(TimeProvider timeProvider) : IRaceTickRegistry
{
    private readonly ConcurrentDictionary<Guid, RacingLobbyEntry> lobbies = new();

    public void RegisterLobby(Guid lobbyId)
    {
        lobbies[lobbyId] = new RacingLobbyEntry(lobbyId, RacePhase.Countdown, timeProvider.GetUtcNow().UtcDateTime);
    }

    public void DeregisterLobby(Guid lobbyId) => lobbies.TryRemove(lobbyId, out _);

    public void TransitionToRacing(Guid lobbyId)
    {
        if (lobbies.TryGetValue(lobbyId, out var entry))
            lobbies[lobbyId] = entry with { Phase = RacePhase.Racing };
    }

    public IReadOnlyList<RacingLobbyEntry> GetRacingLobbies() => lobbies.Values.ToList();
}
