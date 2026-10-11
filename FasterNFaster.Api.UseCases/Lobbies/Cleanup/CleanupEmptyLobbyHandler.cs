using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Cleanup;

public class CleanupEmptyLobbyHandler(EmptyLobbyRemover remover)
    : INotificationHandler<DomainEventNotification<PlayerRemovedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerRemovedEvent> notification, CancellationToken cancellationToken) =>
        await remover.TryRemove(notification.Event.LobbyId);
}
