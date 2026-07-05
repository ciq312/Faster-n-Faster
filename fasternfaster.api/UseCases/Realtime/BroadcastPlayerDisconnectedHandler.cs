using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime;

public class BroadcastPlayerDisconnectedHandler(
    IBroadcaster broadcaster,
    ILobbyStateBroadcaster lobbyState) : INotificationHandler<DomainEventNotification<PlayerDisconnectedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerDisconnectedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        await broadcaster.Broadcast(Audience.Lobby(e.LobbyId), GameEvents.PlayerDisconnected, new PlayerDisconnectedDTO(e.UserId, e.Nick));
        await lobbyState.BroadcastLobbyState(e.LobbyId);
    }
}

public record PlayerDisconnectedDTO(Guid DisconnectedUserId, string DisconnectedUserNick);
