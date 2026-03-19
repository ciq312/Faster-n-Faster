public interface ILobbyService
{
    public void TrackConnection(string connectionId, Guid lobbyId, Guid playerId);
    public void RemoveConnection(string connectionId);

    public (Guid LobbyId, Guid PlayerId)? GetConnection(string connectionId);
}
