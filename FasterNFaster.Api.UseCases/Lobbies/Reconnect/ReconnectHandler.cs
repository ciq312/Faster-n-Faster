using FasterNFaster.Api.UseCases.Lobbies.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Reconnect;

public class ReconnectHandler(IPendingRemovalsRegistry pendingRemovalsRegistry) : IRequestHandler<ReconnectCommand>
{
    public Task Handle(ReconnectCommand command, CancellationToken cancellationToken)
    {
        pendingRemovalsRegistry.TryCancelPendingRemoval(command.PlayerId);
        return Task.CompletedTask;
    }
}
