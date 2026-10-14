using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Lobbies.Services;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Refresh;

public class RefreshHandler(ILobbyStore lobbies, ILobbyStateTracker tracker) : IRequestHandler<RefreshCommand>
{
    public Task Handle(RefreshCommand command, CancellationToken cancellationToken)
    {
        tracker.MarkChanged(lobbies.GetLobbyIdOfPlayerRequired(command.UserId));
        return Task.CompletedTask;
    }
}
