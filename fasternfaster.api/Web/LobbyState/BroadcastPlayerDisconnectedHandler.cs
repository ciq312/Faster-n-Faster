using System.Runtime.CompilerServices;
using FastEndpoints;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Core.Lobbies.Events;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Hubs;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.LobbyState;

public class BroadcastPlayerDisconnectedHandler(LobbyStateBroadcaster broadcaster, IHubContext<GameHub> hub, ISessionService sessionService) : IDomainEventHandler<PlayerDisconnectedEvent>
{
    private readonly LobbyStateBroadcaster broadcaster = broadcaster;
    private readonly IHubContext<GameHub> hub = hub;
    private readonly ISessionService sessionService = sessionService;

    public async Task Handle(PlayerDisconnectedEvent domainEvent)
    {
        var groupName = $"lobby-{domainEvent.LobbyId}";
        var disconnectedConnectionId = sessionService.GetActiveSession(domainEvent.UserId);
        await hub.Groups.RemoveFromGroupAsync(disconnectedConnectionId!, groupName);
        await hub.Clients.Group(groupName).SendAsync("PlayerDisconnected", new PlayerDisconnectedDTO(domainEvent.UserId, domainEvent.Nick));
        await broadcaster.BroadcastLobbyState(domainEvent.LobbyId);
    }
}
public record PlayerDisconnectedDTO(Guid DisconnectedUserId, string DisconnectedUserNick);