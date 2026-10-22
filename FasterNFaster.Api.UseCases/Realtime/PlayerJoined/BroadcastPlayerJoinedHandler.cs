using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.PlayerJoined;

public class BroadcastPlayerJoinedHandler(IBroadcaster broadcaster)
    : INotificationHandler<DomainEventNotification<PlayerJoinedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerJoinedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        await broadcaster.Broadcast(Audience.LobbyExcept(e.LobbyId, e.UserId), c => c.PlayerJoined(new PlayerJoinedDTO(e.UserId, e.Nick)));
    }
}
