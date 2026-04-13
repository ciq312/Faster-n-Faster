namespace FasterNFaster.Api.UseCases.Interfaces;

public interface ILobbyService
{
    public void TrackConnection(string connectionId, Guid lobbyId, Guid playerId);
    public void RemoveConnection(string connectionId);

    public (Guid LobbyId, Guid PlayerId)? GetConnection(string connectionId);
    public bool IsPlayerInLobby(Guid userId);
    public string? GetConnectionId(Guid lobbyId, Guid playerId);

    public void StorePendingRemoval(Guid lobbyId, Guid playerId, CancellationTokenSource cts);
    public bool TryGetPendingRemoval(Guid lobbyId, Guid playerId, out CancellationTokenSource cts);
}
