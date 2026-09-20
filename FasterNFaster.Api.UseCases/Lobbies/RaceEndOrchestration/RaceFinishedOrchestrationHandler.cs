using FasterNFaster.Api.Core.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;

public class RaceFinishedOrchestrationHandler(
    ILobbyAccess lobbies,
    IRaceAccess races,
    IPublisher publisher) : INotificationHandler<DomainEventNotification<RaceFinishedEvent>>
{
    public async Task Handle(DomainEventNotification<RaceFinishedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        await lobbies.Mutate(e.LobbyId, l => l.EndSession());

        await races.RefreshPassage(e.LobbyId);

        await publisher.Publish(new RaceSessionEndedEvent(e.LobbyId, e.Results), cancellationToken);
    }
}
