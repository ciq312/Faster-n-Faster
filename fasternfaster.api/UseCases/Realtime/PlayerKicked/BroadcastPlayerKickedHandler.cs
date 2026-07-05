using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.PlayerKicked;

public class BroadcastPlayerKickedHandler(
    IBroadcaster broadcaster,
    ILobbyStateBroadcaster lobbyState) : INotificationHandler<DomainEventNotification<PlayerKickedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerKickedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        await broadcaster.Broadcast(Audience.Lobby(e.LobbyId), GameEvents.PlayerKicked, new PlayerKickedDTO(e.UserId, e.Nick));
        await broadcaster.Broadcast(Audience.Player(e.UserId), GameEvents.Kicked);
        await lobbyState.BroadcastLobbyState(e.LobbyId);
    }
}
