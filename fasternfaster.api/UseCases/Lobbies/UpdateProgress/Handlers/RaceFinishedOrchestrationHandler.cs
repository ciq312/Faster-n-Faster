using FasterNFaster.Api.Core.Entities.Lobbies.Races.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;

public class RaceFinishedOrchestrationHandler(
    ILobbyStore lobbyStore,
    ILobbySessionService lobbySessionService,
    IPublisher publisher) : INotificationHandler<RaceFinishedEvent>
{
    public async Task Handle(RaceFinishedEvent e, CancellationToken cancellationToken)
    {
        var lobby = lobbyStore.Get(e.LobbyId) ?? throw new LobbyNotFoundException(e.LobbyId);
        lobby.OnSessionEnded();
        await lobbySessionService.RefreshPassage(lobby.HostId);
        await publisher.Publish(new RaceSessionEndedEvent(lobby, e.Results), cancellationToken);
    }
}
