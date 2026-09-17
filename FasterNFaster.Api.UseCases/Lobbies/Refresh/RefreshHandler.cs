using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Refresh;

public class RefreshHandler(
    IPendingRemovalsRegistry pendingRemovalsRegistry,
    ILobbyAccess lobbies,
    IBroadcaster broadcaster,
    ILobbyServiceFacade facade) : IRequestHandler<RefreshCommand>
{
    public async Task Handle(RefreshCommand command, CancellationToken cancellationToken)
    {
        await pendingRemovalsRegistry.TryCancelPendingRemoval(command.UserId);
        var lobby = lobbies.GetOfPlayerRequired(command.UserId);
        Guid lobbyId = lobby.Id;
        await broadcaster.Broadcast(Audience.Lobby(lobbyId), GameEvents.LobbyState, await facade.GetLobbyStateDTO(lobbyId));
    }
}
