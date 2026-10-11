using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.PlayerKicked;

public class BroadcastPlayerKickedHandler(IBroadcaster broadcaster)
    : INotificationHandler<DomainEventNotification<PlayerKickedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerKickedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        await broadcaster.Broadcast(Audience.Lobby(e.LobbyId), GameEvents.PlayerKicked, new PlayerKickedDTO(e.UserId, e.Nick));
        await broadcaster.Broadcast(Audience.Player(e.UserId), GameEvents.Kicked);
    }
}
