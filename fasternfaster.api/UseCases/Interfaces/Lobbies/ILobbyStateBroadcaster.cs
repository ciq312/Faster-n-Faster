using FasterNFaster.Api.Core.Entities.Lobbies;

namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyStateBroadcaster
{
    Task BroadcastLobbyState(Lobby lobby);
    Task BroadcastLobbyState(Guid lobbyId);
}
