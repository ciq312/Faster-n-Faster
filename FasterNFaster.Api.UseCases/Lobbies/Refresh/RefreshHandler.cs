using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Refresh;

public class RefreshHandler(
    IPendingRemovalsRegistry pendingRemovalsRegistry,
    ILobbyAccess lobbies,
    ILobbyStateTracker tracker) : IRequestHandler<RefreshCommand>
{
    public Task Handle(RefreshCommand command, CancellationToken cancellationToken)
    {
        pendingRemovalsRegistry.TryCancelPendingRemoval(command.UserId);

        var lobby = lobbies.GetOfPlayerRequired(command.UserId);

        tracker.MarkChanged(lobby.Id);
        return Task.CompletedTask;
    }
}
