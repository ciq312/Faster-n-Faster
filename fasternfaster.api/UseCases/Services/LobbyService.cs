using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyService : ILobbyService
{
    private readonly ConcurrentDictionary<string, (Guid LobbyId, Guid PlayerId)> _connections =
        new();

    public void TrackConnection(string connectionId, Guid lobbyId, Guid playerId)
    {
        _connections[connectionId] = (lobbyId, playerId);
        Log.Information(
            "Tracking connection {ConnectionId} for player {PlayerId} in lobby {LobbyId}",
            connectionId, playerId, lobbyId);
    }

    public void RemoveConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var removed))
            Log.Information(
                "Removed connection {ConnectionId} for player {PlayerId} in lobby {LobbyId}",
                connectionId, removed.PlayerId, removed.LobbyId);
    }

    public (Guid LobbyId, Guid PlayerId)? GetConnection(string connectionId) =>
        _connections.TryGetValue(connectionId, out var value) ? value : null;

    public string? GetConnectionId(Guid lobbyId, Guid playerId) =>
        _connections.FirstOrDefault(c => c.Value.LobbyId == lobbyId && c.Value.PlayerId == playerId).Key;
}
