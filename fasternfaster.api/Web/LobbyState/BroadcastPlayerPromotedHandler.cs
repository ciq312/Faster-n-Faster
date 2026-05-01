using System.Runtime.CompilerServices;
using FastEndpoints;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Web.Hubs;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.LobbyState;

public class BroadcastPlayerPromotedHandler(LobbyStateBroadcaster broadcaster, IHubContext<GameHub> hub) : IDomainEventHandler<HostChangedEvent>
{
    private readonly LobbyStateBroadcaster broadcaster = broadcaster;
    private readonly IHubContext<GameHub> hub = hub;
    public async Task Handle(HostChangedEvent domainEvent)
    {
#if DEBUG
        Log.Information("host changed event handling");
#endif
        string groupName = $"lobby-{domainEvent.LobbyId}";
        await hub.Clients.Group(groupName).SendAsync("HostChanged", new HostChangedDTO(domainEvent.NewHostId, domainEvent.NewHostNick));
        await broadcaster.BroadcastLobbyState(domainEvent.LobbyId);
    }
}
public record HostChangedDTO(Guid UserId, string NewHostNick);