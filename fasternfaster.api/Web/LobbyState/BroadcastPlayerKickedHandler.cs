using System.Runtime.CompilerServices;
using FastEndpoints;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Hubs;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.LobbyState;

public class BroadcastPlayerKickedHandler(LobbyStateBroadcaster broadcaster, IHubContext<GameHub> hub, ISessionService sessionService) : IDomainEventHandler<PlayerKickedEvent>
{
    private readonly LobbyStateBroadcaster broadcaster = broadcaster;
    private readonly IHubContext<GameHub> hub = hub;
    private readonly ISessionService sessionService = sessionService;
    public async Task Handle(PlayerKickedEvent domainEvent)
    {
        var groupName = $"lobby-{domainEvent.LobbyId}";
        var kickedConnectionId = sessionService.GetActiveSession(domainEvent.UserId);

        if (kickedConnectionId == null) return;

        await hub.Groups.RemoveFromGroupAsync(kickedConnectionId, groupName);
        await hub.Clients.Group(groupName).SendAsync("PlayerKicked", new PlayerKickedDTO(domainEvent.UserId, domainEvent.Nick));
        await hub.Clients.Client(kickedConnectionId).SendAsync("Kicked");

        await broadcaster.BroadcastLobbyState(domainEvent.LobbyId);
    }
}
public record PlayerKickedDTO(Guid UserId, string Nick);