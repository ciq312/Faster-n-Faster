using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyService : ILobbyService
{
    private readonly ConcurrentDictionary<string, (Guid LobbyId, Guid PlayerId)> _connections =
        new();

    private readonly ConcurrentDictionary<(Guid id, Guid lobbyId), CancellationTokenSource> _pendingRemovals = new();

    public void TrackConnection(string connectionId, Guid lobbyId, Guid playerId)
    {
        _connections[connectionId] = (lobbyId, playerId);
#if DEBUG
        Log.Information(
            "Tracking connection {ConnectionId} for player {PlayerId} in lobby {LobbyId}",
            connectionId, playerId, lobbyId);
#endif
    }

    public void RemoveConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var removed))
#if DEBUG
            Log.Information(
                "Removed connection {ConnectionId} for player {PlayerId} in lobby {LobbyId}",
                connectionId, removed.PlayerId, removed.LobbyId);
#endif
    }

    public (Guid LobbyId, Guid PlayerId)? GetConnection(string connectionId) =>
        _connections.TryGetValue(connectionId, out var value) ? value : null;

    public string? GetConnectionId(Guid lobbyId, Guid playerId) =>
        _connections.FirstOrDefault(c => c.Value.LobbyId == lobbyId && c.Value.PlayerId == playerId).Key;

    public void StorePendingRemoval(Guid lobbyId, Guid playerId, CancellationTokenSource cts)
    {
        if (_pendingRemovals.TryAdd((playerId, lobbyId), cts)) return;
        throw new InvalidOperationException($"Storing pending removal failed for player {playerId} in lobby {lobbyId}.");
    }

    public bool TryGetPendingRemoval(Guid lobbyId, Guid playerId, out CancellationTokenSource cts)
    {
        return _pendingRemovals.TryRemove((playerId, lobbyId), out cts!);
    }
}
