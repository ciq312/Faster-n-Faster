using System.Collections.Concurrent;

public class LobbbyService : ILobbyService
{
    private readonly ConcurrentDictionary<string, (Guid LobbyId, Guid PlayerId)> _connections =
        new();

    public void TrackConnection(string connectionId, Guid lobbyId, Guid playerId) =>
        _connections[connectionId] = (lobbyId, playerId);

    public void RemoveConnection(string connectionId) =>
        _connections.TryRemove(connectionId, out _);

    public (Guid LobbyId, Guid PlayerId)? GetConnection(string connectionId) =>
        _connections.TryGetValue(connectionId, out var value) ? value : null;
}
