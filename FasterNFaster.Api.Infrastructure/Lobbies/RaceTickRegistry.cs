using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;

namespace FasterNFaster.Api.Infrastructure.Lobbies;

public class RaceTickRegistry : IRaceTickRegistry
{
    private readonly ConcurrentDictionary<Guid, RacingLobbyEntry> lobbies = new();

    public void RegisterLobby(Guid lobbyId)
    {
        lobbies[lobbyId] = new RacingLobbyEntry(lobbyId, RacePhase.Countdown, DateTime.UtcNow);
    }

    public void DeregisterLobby(Guid lobbyId) => lobbies.TryRemove(lobbyId, out _);

    public void TransitionToRacing(Guid lobbyId)
    {
        if (lobbies.TryGetValue(lobbyId, out var entry))
            lobbies[lobbyId] = entry with { Phase = RacePhase.Racing };
    }

    public IReadOnlyList<RacingLobbyEntry> GetRacingLobbies() => lobbies.Values.ToList();
}
