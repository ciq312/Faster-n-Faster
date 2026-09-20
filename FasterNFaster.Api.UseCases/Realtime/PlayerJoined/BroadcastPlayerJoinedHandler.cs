using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.PlayerJoined;

public class BroadcastPlayerJoinedHandler(IBroadcaster broadcaster)
    : INotificationHandler<DomainEventNotification<PlayerJoinedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerJoinedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        await broadcaster.Broadcast(Audience.LobbyExcept(e.LobbyId, e.UserId), GameEvents.PlayerJoined, new PlayerJoinedDTO(e.UserId, e.Nick));
    }
}
