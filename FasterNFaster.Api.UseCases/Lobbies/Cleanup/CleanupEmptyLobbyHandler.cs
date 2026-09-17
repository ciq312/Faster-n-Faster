using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Cleanup;

public class CleanupEmptyLobbyHandler(
    ILobbyAccess lobbies,
    IRaceService raceService,
    IRaceTickRegistry raceTickRegistry)
    : INotificationHandler<DomainEventNotification<PlayerRemovedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerRemovedEvent> notification, CancellationToken cancellationToken)
    {
        Guid lobbyId = notification.Event.LobbyId;
        Lobby lobby = lobbies.GetRequired(lobbyId);

        if (!lobby.IsEmpty()) return;

        await lobbies.Remove(lobbyId);

        raceService.RemoveRegisteredRace(lobbyId);

        raceTickRegistry.DeregisterLobby(lobbyId);
    }
}
