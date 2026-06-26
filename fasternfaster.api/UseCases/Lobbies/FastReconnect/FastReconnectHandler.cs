using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.FastReconnect;

public class FastReconnectHandler(IPendingRemovalsRegistry pendingRemovalsRegistry) : IRequestHandler<FastReconnectCommand>
{
    private const int ReconnectGracePeriodSeconds = 15;

    public async Task Handle(FastReconnectCommand command, CancellationToken cancellationToken)
    {
        var cts = new CancellationTokenSource();

        await pendingRemovalsRegistry.StorePendingRemoval(command.PlayerId, cts);
        try
        {
            await Task.Delay(ReconnectGracePeriodSeconds * 1000, cts.Token);
        }
        finally
        {
            await pendingRemovalsRegistry.RemovePendingRemoval(command.PlayerId);
        }
    }
}

public record FastReconnectCommand(Guid LobbyId, Guid PlayerId) : IRequest;
