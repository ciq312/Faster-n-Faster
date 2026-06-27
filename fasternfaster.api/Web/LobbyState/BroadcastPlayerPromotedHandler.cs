using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.Web.Hubs;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.LobbyState;

public class BroadcastPlayerPromotedHandler(LobbyStateBroadcaster broadcaster, IHubContext<GameHub> hub)
    : INotificationHandler<DomainEventNotification<HostChangedEvent>>
{
    public async Task Handle(DomainEventNotification<HostChangedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;
        string groupName = LobbyGroup(e.LobbyId);
        await hub.Clients.Group(groupName).SendAsync(Methods.HostChanged, new HostChangedDTO(e.NewHostId, e.NewHostNick));
        await broadcaster.BroadcastLobbyState(e.LobbyId);
    }
}

public record HostChangedDTO(Guid UserId, string NewHostNick);
