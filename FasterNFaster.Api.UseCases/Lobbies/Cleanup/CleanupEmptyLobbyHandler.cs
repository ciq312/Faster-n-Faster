using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Cleanup;

public class CleanupEmptyLobbyHandler(
    ILobbyAccess lobbies,
    IRaceAccess races,
    IRaceTickRegistry raceTickRegistry)
    : INotificationHandler<DomainEventNotification<PlayerRemovedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerRemovedEvent> notification, CancellationToken cancellationToken)
    {
        var lobbyId = notification.Event.LobbyId;
        var lobby = lobbies.GetRequired(lobbyId);

        if (!lobby.IsEmpty()) return;

        raceTickRegistry.DeregisterLobby(lobbyId);

        await lobbies.Remove(lobbyId);

        races.Remove(lobbyId);
    }
}
