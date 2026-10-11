using FasterNFaster.Api.UseCases.Lobbies.LobbyState;

namespace FasterNFaster.Api.UseCases.Lobbies.Services;

public interface ILobbyQuery
{
    Task<LobbyStateDTO> GetLobbyState(Guid lobbyId);
}
