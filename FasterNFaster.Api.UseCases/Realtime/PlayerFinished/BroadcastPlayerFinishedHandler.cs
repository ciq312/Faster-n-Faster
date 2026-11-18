using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.PlayerFinished;

public class BroadcastPlayerFinishedHandler(IBroadcaster broadcaster)
    : INotificationHandler<DomainEventNotification<PlayerFinishedEvent>>
{
    public Task Handle(DomainEventNotification<PlayerFinishedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;
        return broadcaster.Broadcast(Audience.Lobby(e.LobbyId),
            c => c.PlayerFinished(new PlayerFinishedDTO(e.Nick, e.PlayerId, e.FinishPosition, e.Wpm, e.Accuracy)));
    }
}
