using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Cleanup;

public class CleanupEmptyLobbyHandler(ILobbyServiceFacade facade)
    : INotificationHandler<DomainEventNotification<PlayerRemovedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerRemovedEvent> notification, CancellationToken cancellationToken)
    {
        await facade.RemoveLobbyIfEmpty(notification.Event.LobbyId);
    }
}
