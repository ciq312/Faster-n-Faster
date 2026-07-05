using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime;

public class BroadcastPlayerPromotedHandler(
    IBroadcaster broadcaster,
    ILobbyStateBroadcaster lobbyState) : INotificationHandler<DomainEventNotification<HostChangedEvent>>
{
    public async Task Handle(DomainEventNotification<HostChangedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        await broadcaster.Broadcast(Audience.Lobby(e.LobbyId), GameEvents.HostChanged, new HostChangedDTO(e.NewHostId, e.NewHostNick));
        await lobbyState.BroadcastLobbyState(e.LobbyId);
    }
}

public record HostChangedDTO(Guid UserId, string NewHostNick);
