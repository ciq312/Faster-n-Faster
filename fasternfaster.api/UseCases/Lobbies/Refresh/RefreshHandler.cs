using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Refresh;

public class RefreshHandler(
    IPendingRemovalsRegistry pendingRemovalsRegistry,
    ILobbyService lobbyService,
    ILobbyStateBroadcaster broadcaster) : IRequestHandler<RefreshCommand>
{
    public async Task Handle(RefreshCommand command, CancellationToken cancellationToken)
    {
        await pendingRemovalsRegistry.TryCancelPendingRemoval(command.UserId);
        var lobby = lobbyService.GetLobbyOfPlayerRequired(command.UserId);
        await broadcaster.BroadcastLobbyState(lobby);
    }
}
