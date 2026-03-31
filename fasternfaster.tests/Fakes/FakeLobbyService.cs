using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Tests.Fakes;

public class FakeLobbyService : ILobbyService
{
    private readonly Dictionary<string, (Guid LobbyId, Guid PlayerId)> _connections = new();

    public void TrackConnection(string connectionId, Guid lobbyId, Guid playerId)
        => _connections[connectionId] = (lobbyId, playerId);

    public void RemoveConnection(string connectionId)
        => _connections.Remove(connectionId);

    public (Guid LobbyId, Guid PlayerId)? GetConnection(string connectionId)
        => _connections.TryGetValue(connectionId, out var val) ? val : null;

    public string? GetConnectionId(Guid lobbyId, Guid playerId)
        => _connections.FirstOrDefault(c => c.Value.LobbyId == lobbyId && c.Value.PlayerId == playerId).Key;
}
