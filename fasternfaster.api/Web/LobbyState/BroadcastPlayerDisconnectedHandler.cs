using FasterNFaster.Api.Core.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.Web.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.LobbyState;

public class BroadcastPlayerDisconnectedHandler(
    ILobbyStateBroadcaster broadcaster,
    IHubContext<GameHub> hub,
    ISessionService sessionService) : INotificationHandler<DomainEventNotification<PlayerDisconnectedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerDisconnectedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;
        var groupName = LobbyGroup(e.LobbyId);
        var disconnectedConnectionId = sessionService.GetActiveSession(e.UserId);
        await hub.Groups.RemoveFromGroupAsync(disconnectedConnectionId!, groupName);
        await hub.Clients.Group(groupName).SendAsync(Methods.PlayerDisconnected, new PlayerDisconnectedDTO(e.UserId, e.Nick));
        await broadcaster.BroadcastLobbyState(e.LobbyId);
    }
}

public record PlayerDisconnectedDTO(Guid DisconnectedUserId, string DisconnectedUserNick);
