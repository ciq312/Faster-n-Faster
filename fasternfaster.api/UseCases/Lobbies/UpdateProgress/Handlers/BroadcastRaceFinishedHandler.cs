using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Infrastructure.Hubs;
using FasterNFaster.Api.UseCases.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;

public class BroadcastRaceFinishedHandler : IDomainEventHandler<RaceFinishedEvent>
{
    private readonly IHubContext<GameHub> _hub;
    private readonly ILobbyStore _lobbyStore;
    private readonly LobbyStateBroadcaster _broadcaster;

    private readonly IPassageProvider _passageProvider;

    public BroadcastRaceFinishedHandler(IHubContext<GameHub> hub, ILobbyStore lobbyStore, LobbyStateBroadcaster broadcaster, IPassageProvider passageProvider)
    {
        _hub = hub;
        _lobbyStore = lobbyStore;
        _broadcaster = broadcaster;
        _passageProvider = passageProvider;
    }

    public async Task Handle(RaceFinishedEvent e)
    {
        Log.Logger.Information($"race ended in lobby {e.lobbyId}");
        await _hub.Clients.Group($"lobby-{e.lobbyId}").SendAsync("RaceEnded", new
        {
            results = e.results
        });

        var lobby = _lobbyStore.Get(e.lobbyId) ?? throw new LobbyNotFoundException(e.lobbyId);

        lobby.RaceSettings.SetPassage(await _passageProvider.GetPassageAsync(lobby.RaceSettings.WordCount));

        if (lobby != null)
            await _broadcaster.BroadcastLobbyState(lobby);
    }
}
