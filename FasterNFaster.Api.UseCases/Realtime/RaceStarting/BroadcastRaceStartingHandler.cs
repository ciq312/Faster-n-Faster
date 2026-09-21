using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.RaceStarting;

public class BroadcastRaceStartingHandler(IBroadcaster broadcaster)
    : INotificationHandler<DomainEventNotification<SessionStartedEvent>>
{
    private const int CountdownSeconds = 3;

    public Task Handle(DomainEventNotification<SessionStartedEvent> notification, CancellationToken cancellationToken) =>
        broadcaster.Broadcast(Audience.Lobby(notification.Event.LobbyId), GameEvents.RaceStarting, new RaceStartingDTO(CountdownSeconds));
}
