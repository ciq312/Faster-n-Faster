using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.UseCases.Lobbies.HostChanged.Handlers;

public class BroadcastHostChangedHandler(IHubContext<GameHub> hub) : IDomainEventHandler<HostChangedEvent>
{
    private readonly IHubContext<GameHub> hub = hub;

    public async Task Handle(HostChangedEvent e)
    {
        await hub.Clients.Group($"lobby-{e.LobbyId}")
            .SendAsync("HostChanged", new { newHostNick = e.NewHostNick, newHostPlayerId = e.NewHostId });
    }
}
