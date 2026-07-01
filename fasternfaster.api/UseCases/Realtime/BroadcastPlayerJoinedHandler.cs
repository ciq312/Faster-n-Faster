using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime;

public class BroadcastPlayerJoinedHandler(
    IBroadcaster broadcaster,
    ILobbyStateBroadcaster lobbyState) : INotificationHandler<DomainEventNotification<PlayerJoinedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerJoinedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        await lobbyState.BroadcastLobbyState(e.LobbyId);
        await broadcaster.Broadcast(Audience.LobbyExcept(e.LobbyId, e.UserId), GameEvents.PlayerJoined, new PlayerJoinedDTO(e.UserId, e.Nick));
    }
}

public record PlayerJoinedDTO(Guid PlayerId, string DisplayName);
