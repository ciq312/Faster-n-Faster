using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Services.Races;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.RaceStarting;

public class BroadcastRaceStartingHandler(IBroadcaster broadcaster)
    : INotificationHandler<DomainEventNotification<SessionStartedEvent>>
{
    public Task Handle(DomainEventNotification<SessionStartedEvent> notification, CancellationToken cancellationToken) =>
        broadcaster.Broadcast(Audience.Lobby(notification.Event.LobbyId), GameEvents.RaceStarting, new RaceStartingDTO((int)RaceCountdown.Duration.TotalSeconds));
}
