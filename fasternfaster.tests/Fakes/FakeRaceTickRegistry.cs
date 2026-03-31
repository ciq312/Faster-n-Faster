using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Tests.Fakes;

public class FakeRaceTickRegistry : IRaceTickRegistry
{
    private readonly Dictionary<Guid, RacingLobbyEntry> _lobbies = new();

    public void RegisterLobby(Guid lobbyId)
        => _lobbies[lobbyId] = new RacingLobbyEntry(lobbyId, RacePhase.Countdown, DateTime.UtcNow);

    public void DeregisterLobby(Guid lobbyId)
        => _lobbies.Remove(lobbyId);

    public void TransitionToRacing(Guid lobbyId)
    {
        if (_lobbies.TryGetValue(lobbyId, out var entry))
            _lobbies[lobbyId] = entry with { Phase = RacePhase.Racing };
    }

    public RacingLobbyEntry GetRacingLobby(Guid lobbyId)
        => _lobbies[lobbyId];

    public IReadOnlyList<RacingLobbyEntry> GetRacingLobbies()
        => _lobbies.Values.ToList();

    public bool IsRegistered(Guid lobbyId) => _lobbies.ContainsKey(lobbyId);
}
