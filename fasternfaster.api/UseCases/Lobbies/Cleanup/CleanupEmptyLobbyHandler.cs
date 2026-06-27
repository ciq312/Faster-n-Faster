using FasterNFaster.Api.Core.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Cleanup;

public class CleanupEmptyLobbyHandler(ILobbyServiceFacade lobbySessionService)
    : INotificationHandler<DomainEventNotification<PlayerRemovedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerRemovedEvent> notification, CancellationToken cancellationToken)
    {
        await lobbySessionService.RemoveLobbyIfEmpty(notification.Event.LobbyId);
    }
}
