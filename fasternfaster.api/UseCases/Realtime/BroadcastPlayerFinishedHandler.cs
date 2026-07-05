using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime;

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

public record PlayerFinishedDTO(string Nick, Guid PlayerId, int? FinishPosition, double Wpm, double Accuracy);
