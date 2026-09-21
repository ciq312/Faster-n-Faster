using FasterNFaster.Api.UseCases.LobbyState;

namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyQuery
{
    Task<LobbyStateDTO> GetLobbyState(Guid lobbyId);
}
