using FasterNFaster.Api.Core.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;

public class RaceFinishedOrchestrationHandler(
    ILobbyStore lobbyStore,
    ILobbyServiceFacade lobbySessionService,
    IPublisher publisher) : INotificationHandler<DomainEventNotification<RaceFinishedEvent>>
{
    public async Task Handle(DomainEventNotification<RaceFinishedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;
        await lobbySessionService.EndSession(e.LobbyId);

        var lobby = lobbyStore.Get(e.LobbyId) ?? throw new LobbyNotFoundException(e.LobbyId);
        await lobbySessionService.RefreshPassage(lobby.HostId);
        await publisher.Publish(new RaceSessionEndedEvent(lobby, e.Results), cancellationToken);
    }
}
