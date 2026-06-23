using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Core.Lobbies.Events;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Hubs;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.LobbyState;

public class BroadcastPlayerDisconnectedHandler(LobbyStateBroadcaster broadcaster, IHubContext<GameHub> hub, ISessionService sessionService) : IDomainEventHandler<PlayerDisconnectedEvent>
{
    private readonly LobbyStateBroadcaster broadcaster = broadcaster;
    private readonly IHubContext<GameHub> hub = hub;
    private readonly ISessionService sessionService = sessionService;

    public async Task Handle(PlayerDisconnectedEvent domainEvent)
    {
        var groupName = LobbyGroup(domainEvent.LobbyId);
        var disconnectedConnectionId = sessionService.GetActiveSession(domainEvent.UserId);
        await hub.Groups.RemoveFromGroupAsync(disconnectedConnectionId!, groupName);
        await hub.Clients.Group(groupName).SendAsync(Methods.PlayerDisconnected, new PlayerDisconnectedDTO(domainEvent.UserId, domainEvent.Nick));
        await broadcaster.BroadcastLobbyState(domainEvent.LobbyId);
    }
}

public record PlayerDisconnectedDTO(Guid DisconnectedUserId, string DisconnectedUserNick);
