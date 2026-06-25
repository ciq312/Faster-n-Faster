using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Hubs;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.LobbyState;

public class BroadcastPlayerKickedHandler(LobbyStateBroadcaster broadcaster, IHubContext<GameHub> hub, ISessionService sessionService) : INotificationHandler<PlayerKickedEvent>
{
    private readonly LobbyStateBroadcaster broadcaster = broadcaster;
    private readonly IHubContext<GameHub> hub = hub;
    private readonly ISessionService sessionService = sessionService;

    public async Task Handle(PlayerKickedEvent domainEvent, CancellationToken cancellationToken)
    {
        var groupName = LobbyGroup(domainEvent.LobbyId);
        var kickedConnectionId = sessionService.GetActiveSession(domainEvent.UserId);

        if (kickedConnectionId == null) return;

        await hub.Groups.RemoveFromGroupAsync(kickedConnectionId, groupName);
        await hub.Clients.Group(groupName).SendAsync(Methods.PlayerKicked, new PlayerKickedDTO(domainEvent.UserId, domainEvent.Nick));
        await hub.Clients.Client(kickedConnectionId).SendAsync(Methods.Kicked);

        await broadcaster.BroadcastLobbyState(domainEvent.LobbyId);
    }
}

public record PlayerKickedDTO(Guid UserId, string Nick);
