namespace FasterNFaster.Api.UseCases.Lobbies.Ports;

public interface IPlayerLocationRegistry
{
    Guid? GetLobbyIdOfPlayer(Guid userId);
    Guid GetLobbyIdOfPlayerRequired(Guid userId);
    void Track(Guid userId, Guid lobbyId);
    void Untrack(Guid userId);
}
