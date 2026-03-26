using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;

public class BroadcastRaceFinishedHandler : IDomainEventHandler<RaceFinishedEvent>
{
    private readonly IHubContext<GameHub> _hub;

    public BroadcastRaceFinishedHandler(IHubContext<GameHub> hub)
    {
        _hub = hub;
    }

    public async Task Handle(RaceFinishedEvent e)
    {
        Log.Logger.Information($"race ended in lobby {e.lobbyId}");
        await _hub.Clients.Group($"lobby-{e.lobbyId}").SendAsync("RaceEnded", new
        {
            results = e.results
        });
    }
}
