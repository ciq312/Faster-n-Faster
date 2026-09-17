using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;

public class RaceFinishedOrchestrationHandler(
    ILobbyAccess lobbies,
    IRaceInternals raceInternals,
    IPublisher publisher) : INotificationHandler<DomainEventNotification<RaceFinishedEvent>>
{
    public async Task Handle(DomainEventNotification<RaceFinishedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        await lobbies.Mutate(e.LobbyId, l => l.EndSession());

        // Prepares the passage for the next race; the host-initiated path is RefreshPassageCommand.
        await raceInternals.RefreshPassage(e.LobbyId);

        Lobby lobby = lobbies.GetRequired(e.LobbyId);

        await publisher.Publish(new RaceSessionEndedEvent(lobby, e.Results), cancellationToken);
    }
}
