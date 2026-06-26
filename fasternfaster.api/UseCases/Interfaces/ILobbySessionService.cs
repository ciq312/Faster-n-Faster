namespace FasterNFaster.Api.UseCases.Interfaces;

public interface ILobbySessionService
{
    public Task RefreshPassage(Guid userId);

    public Task KickPlayer(Guid hostId, Guid userId);

    public Task RemovePlayerFromLobby(Guid userId);

    public Task StartSession(Guid hostId);
    
    public Task RemoveLobbyIfEmpty(Guid lobbyId);

}