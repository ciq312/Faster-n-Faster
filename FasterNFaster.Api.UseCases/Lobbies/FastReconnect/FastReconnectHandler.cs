using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.FastReconnect;

public class FastReconnectHandler(IPendingRemovalsRegistry pendingRemovalsRegistry, TimeProvider timeProvider) : IRequestHandler<FastReconnectCommand>
{
    public static readonly TimeSpan ReconnectGracePeriod = TimeSpan.FromSeconds(15);

    public async Task Handle(FastReconnectCommand command, CancellationToken cancellationToken)
    {
        var cts = new CancellationTokenSource();
        // The registry may dispose the CTS once stored, so the token is read first.
        var token = cts.Token;

        pendingRemovalsRegistry.StorePendingRemoval(command.PlayerId, cts);
        try
        {
            await Task.Delay(ReconnectGracePeriod, timeProvider, token);
        }
        finally
        {
            if (pendingRemovalsRegistry.CompletePendingRemoval(command.PlayerId, cts))
                cts.Dispose();
        }
    }
}
