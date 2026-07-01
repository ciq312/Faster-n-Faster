using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime;

public class BroadcastPlayerKickedHandler(
    IBroadcaster broadcaster,
    ILobbyChannel lobbyChannel,
    ILobbyStateBroadcaster lobbyState) : INotificationHandler<DomainEventNotification<PlayerKickedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerKickedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        await lobbyChannel.Leave(e.UserId, e.LobbyId);
        await broadcaster.Broadcast(Audience.LobbyExcept(e.LobbyId, e.UserId), GameEvents.PlayerKicked, new PlayerKickedDTO(e.UserId, e.Nick));
        await broadcaster.Broadcast(Audience.Player(e.UserId), GameEvents.Kicked);
        await lobbyState.BroadcastLobbyState(e.LobbyId);
    }
}

public record PlayerKickedDTO(Guid UserId, string Nick);
