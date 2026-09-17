using FasterNFaster.Api.UseCases.LobbyState;

namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyServiceFacade
{
    public Task RefreshPassage(Guid userId);

    public Task KickPlayer(Guid hostId, Guid userId);

    public Task RemovePlayerFromLobby(Guid userId);

    public Task EndSession(Guid lobbyId);

    public Task RemoveLobbyIfEmpty(Guid lobbyId);

    public Task UpdateProgress(Guid userId, int index, int mistakes, string typed);

    public bool DoesLobbyExist(Guid lobbyId);

    public Task<LobbyStateDTO> GetLobbyStateDTO(Guid lobbyId);
}
