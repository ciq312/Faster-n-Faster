using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.UseCases.Services;

public class RaceTickRegistry : IRaceTickRegistry
{
    private readonly ConcurrentDictionary<Guid, RacingLobbyEntry> _lobbies = new();

    public void RegisterLobby(Guid lobbyId)
    {
        _lobbies[lobbyId] = new RacingLobbyEntry(lobbyId, RacePhase.Countdown, DateTime.UtcNow);
    }

    public void DeregisterLobby(Guid lobbyId)
    {
        _lobbies.TryRemove(lobbyId, out _);
    }

    public void TransitionToRacing(Guid lobbyId)
    {
        if (_lobbies.TryGetValue(lobbyId, out var entry))
            _lobbies[lobbyId] = entry with { Phase = RacePhase.Racing };
    }

    public IReadOnlyList<RacingLobbyEntry> GetRacingLobbies()
    {
        return _lobbies.Values.ToList();
    }
}
