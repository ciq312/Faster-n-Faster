namespace FasterNFaster.Api.UseCases.Interfaces.Realtime;

public interface ILobbyChannel
{
    Task Join(Guid userId, Guid lobbyId);
    Task Leave(Guid userId, Guid lobbyId);
}
