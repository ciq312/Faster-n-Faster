using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Hubs;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.LobbyState;

public class BroadcastPlayerKickedHandler(
    LobbyStateBroadcaster broadcaster,
    IHubContext<GameHub> hub,
    ISessionService sessionService) : INotificationHandler<DomainEventNotification<PlayerKickedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerKickedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;
        var groupName = LobbyGroup(e.LobbyId);
        var kickedConnectionId = sessionService.GetActiveSession(e.UserId);

        if (kickedConnectionId == null) return;

        await hub.Groups.RemoveFromGroupAsync(kickedConnectionId, groupName);
        await hub.Clients.Group(groupName).SendAsync(Methods.PlayerKicked, new PlayerKickedDTO(e.UserId, e.Nick));
        await hub.Clients.Client(kickedConnectionId).SendAsync(Methods.Kicked);
        await broadcaster.BroadcastLobbyState(e.LobbyId);
    }
}

public record PlayerKickedDTO(Guid UserId, string Nick);
