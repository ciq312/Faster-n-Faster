using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.PlayerDisconnected;

public class BroadcastPlayerDisconnectedHandler(
    IBroadcaster broadcaster,
    ILobbyAccess lobbies) : INotificationHandler<DomainEventNotification<PlayerDisconnectedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerDisconnectedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        if (!lobbies.Exists(e.LobbyId)) return;

        await broadcaster.Broadcast(Audience.Lobby(e.LobbyId), GameEvents.PlayerDisconnected, new PlayerDisconnectedDTO(e.UserId, e.Nick));
    }
}
