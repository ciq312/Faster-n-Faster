namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyInternals
{
    Task ValidateHost(Guid lobbyId, Guid hostId);
    Task RemoveFromLobby(Guid userId);
    Task KickPlayer(Guid hostId, Guid userId);
    Task RemoveLobby(Guid lobbyId);
    Task StartSession(Guid lobbyId, Guid hostId);
}