using FasterNFaster.Api.UseCases.Realtime.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.AntiCheat.Sanctions;

public class PlayerSanctionedHandler(IConnectionTerminator terminator) : INotificationHandler<PlayerSanctionedEvent>
{
    public Task Handle(PlayerSanctionedEvent notification, CancellationToken cancellationToken)
    {
        terminator.Terminate(notification.PlayerId);
        return Task.CompletedTask;
    }
}
