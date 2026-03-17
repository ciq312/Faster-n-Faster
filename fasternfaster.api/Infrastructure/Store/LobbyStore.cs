using System.Collections.Concurrent;
using FasterNFaster.Api.Core.RaceState;

namespace FasterNFaster.Api.Infrastructure.Store;

/// <summary>
/// In-memory store for live lobby and race state. Registered as a singleton.
/// </summary>
public class LobbyStore
{
    // lobbyId → active race state (null when no race running)
    private readonly ConcurrentDictionary<Guid, LobbyRaceState> _races = new();

    // connectionId → (lobbyId, playerId) for quick hub lookup
    private readonly ConcurrentDictionary<string, (Guid LobbyId, Guid PlayerId)> _connections = new();

    public void TrackConnection(string connectionId, Guid lobbyId, Guid playerId) =>
        _connections[connectionId] = (lobbyId, playerId);

    public void RemoveConnection(string connectionId) =>
        _connections.TryRemove(connectionId, out _);

    public (Guid LobbyId, Guid PlayerId)? GetConnection(string connectionId) =>
        _connections.TryGetValue(connectionId, out var value) ? value : null;

    public void StartRace(LobbyRaceState state) =>
        _races[state.LobbyId] = state;

    public LobbyRaceState? GetRace(Guid lobbyId) =>
        _races.GetValueOrDefault(lobbyId);

    public void EndRace(Guid lobbyId) =>
        _races.TryRemove(lobbyId, out _);
}
