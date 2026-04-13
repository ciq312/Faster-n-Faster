using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyService : ILobbyService
{
    private readonly ConcurrentDictionary<string, (Guid LobbyId, Guid PlayerId)> connections =
        new();

    private readonly ConcurrentDictionary<Guid, Guid> playerLobbies = new();
    private readonly ConcurrentDictionary<(Guid id, Guid lobbyId), CancellationTokenSource> pendingRemovals = new();

    public void TrackConnection(string connectionId, Guid lobbyId, Guid playerId)
    {
        connections[connectionId] = (lobbyId, playerId);
        playerLobbies[playerId] = lobbyId;
#if DEBUG
        Log.Information(
            "Tracking connection {ConnectionId} for player {PlayerId} in lobby {LobbyId}",
            connectionId, playerId, lobbyId);
#endif
    }

    public void RemoveConnection(string connectionId)
    {
        if (!connections.TryRemove(connectionId, out var removed)) throw new KeyNotFoundException("No connection found");
        if (!playerLobbies.TryRemove(removed.PlayerId, out var lobbyId)) throw new KeyNotFoundException($"User {removed.PlayerId} is not in lobby");
#if DEBUG
        Log.Information(
            "Removed connection {ConnectionId} for player {PlayerId} in lobby {LobbyId}",
            connectionId, removed.PlayerId, removed.LobbyId);
#endif
    }

    public bool IsPlayerInLobby(Guid userId) => playerLobbies.ContainsKey(userId);

    public (Guid LobbyId, Guid PlayerId)? GetConnection(string connectionId) =>
        connections.TryGetValue(connectionId, out var value) ? value : null;

    public string? GetConnectionId(Guid lobbyId, Guid playerId) =>
        connections.FirstOrDefault(c => c.Value.LobbyId == lobbyId && c.Value.PlayerId == playerId).Key;

    public void StorePendingRemoval(Guid lobbyId, Guid playerId, CancellationTokenSource cts)
    {
        if (pendingRemovals.TryAdd((playerId, lobbyId), cts)) return;
        throw new InvalidOperationException($"Storing pending removal failed for player {playerId} in lobby {lobbyId}.");
    }

    public bool TryGetPendingRemoval(Guid lobbyId, Guid playerId, out CancellationTokenSource cts)
    {
        return pendingRemovals.TryRemove((playerId, lobbyId), out cts!);
    }
}
