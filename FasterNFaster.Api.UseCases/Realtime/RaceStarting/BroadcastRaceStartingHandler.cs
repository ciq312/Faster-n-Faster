using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Services.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.RaceStarting;

public class BroadcastRaceStartingHandler(IBroadcaster broadcaster)
    : INotificationHandler<DomainEventNotification<SessionStartedEvent>>
{
    public Task Handle(DomainEventNotification<SessionStartedEvent> notification, CancellationToken cancellationToken) =>
        broadcaster.Broadcast(Audience.Lobby(notification.Event.LobbyId), GameEvents.RaceStarting, new RaceStartingDTO((int)RaceCountdown.Duration.TotalSeconds));
}
