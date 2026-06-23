using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;

public class BroadcastPlayerFinishedHandler(IHubContext<GameHub> hub) : IDomainEventHandler<PlayerFinishedEvent>
{
    private readonly IHubContext<GameHub> _hub = hub;

    public async Task Handle(PlayerFinishedEvent e)
    {
        await _hub.Clients.Group(LobbyGroup(e.LobbyId)).SendAsync(Methods.PlayerFinished, new
        {
            nick = e.Nick,
            playerId = e.PlayerId,
            finishPosition = e.FinishPosition,
            wpm = e.Wpm,
            accuracy = e.Accuracy
        });
    }
}
