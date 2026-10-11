using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.PlayerFinished;

public class BroadcastPlayerFinishedHandler(IBroadcaster broadcaster)
    : INotificationHandler<DomainEventNotification<PlayerFinishedEvent>>
{
    public Task Handle(DomainEventNotification<PlayerFinishedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;
        return broadcaster.Broadcast(Audience.Lobby(e.LobbyId), GameEvents.PlayerFinished,
            new PlayerFinishedDTO(e.Nick, e.PlayerId, e.FinishPosition, e.Wpm, e.Accuracy));
    }
}
