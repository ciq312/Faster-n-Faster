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

        tracker.MarkChanged(lobbies.GetLobbyIdOfPlayerRequired(command.UserId));
        return Task.CompletedTask;
    }
}
