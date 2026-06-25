using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Web.Hubs;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.LobbyState;

public class BroadcastPlayerPromotedHandler(LobbyStateBroadcaster broadcaster, IHubContext<GameHub> hub) : INotificationHandler<HostChangedEvent>
{
    private readonly LobbyStateBroadcaster broadcaster = broadcaster;
    private readonly IHubContext<GameHub> hub = hub;

    public async Task Handle(HostChangedEvent domainEvent, CancellationToken cancellationToken)
    {
        string groupName = LobbyGroup(domainEvent.LobbyId);
        await hub.Clients.Group(groupName).SendAsync(Methods.HostChanged, new HostChangedDTO(domainEvent.NewHostId, domainEvent.NewHostNick));
        await broadcaster.BroadcastLobbyState(domainEvent.LobbyId);
    }
}

public record HostChangedDTO(Guid UserId, string NewHostNick);
