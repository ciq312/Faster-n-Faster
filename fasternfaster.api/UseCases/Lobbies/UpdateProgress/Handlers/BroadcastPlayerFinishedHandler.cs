using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;

public class BroadcastPlayerFinishedHandler : IDomainEventHandler<PlayerFinishedEvent>
{
    private readonly IHubContext<GameHub> _hub;

    public BroadcastPlayerFinishedHandler(IHubContext<GameHub> hub)
    {
        _hub = hub;
    }

    public async Task Handle(PlayerFinishedEvent e)
    {
        Log.Logger.Information($"player finished race in lobby {e.LobbyId}");

        await _hub.Clients.Group($"lobby-{e.LobbyId}").SendAsync("PlayerFinished", new
        {
            nick = e.Nick,
            playerId = e.PlayerId,
            finishPosition = e.FinishPosition,
            wpm = e.Wpm,
            accuracy = e.Accuracy
        });
    }
}
