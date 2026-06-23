using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Api.UseCases.Lobbies.FastReconnect;

public class FastReconnectHandler(IPendingRemovalsRegistry pendingRemovalsRegistry) : IHandler<FastReconnectCommand>
{
    private readonly IPendingRemovalsRegistry pendingRemovalsRegistry = pendingRemovalsRegistry;

    private readonly int RECONNECT_GRACE_PERIOD_SECONDS = 15;

    public async Task Handle(FastReconnectCommand command)
    {
        var cts = new CancellationTokenSource();

        await pendingRemovalsRegistry.StorePendingRemoval(command.PlayerId, cts);
        try
        {
            await Task.Delay(RECONNECT_GRACE_PERIOD_SECONDS * 1000, cts.Token);
        }
        finally
        {
            await pendingRemovalsRegistry.RemovePendingRemoval(command.PlayerId);
        }


    }
}



public record FastReconnectCommand(Guid LobbyId, Guid PlayerId);
