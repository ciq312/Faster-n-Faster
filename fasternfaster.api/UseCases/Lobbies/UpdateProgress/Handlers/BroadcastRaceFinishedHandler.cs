using FasterNFaster.Api.Core.Entities.Lobbies.Races.Events;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Infrastructure.Hubs;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
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
#if DEBUG
        Log.Logger.Information($"race ended in lobby {e.LobbyId}");
#endif
        await _hub.Clients.Group($"lobby-{e.LobbyId}").SendAsync("RaceEnded", new
        {
            results = e.Results
        });

        var lobby = _lobbyStore.Get(e.LobbyId) ?? throw new LobbyNotFoundException(e.LobbyId);

        lobby.OnSessionEnded();

        var race = lobby.Race;
        race.SetPassage(await _passageProvider.GetPassageAsync(race.WordCount));

        if (lobby != null)
            await _broadcaster.BroadcastLobbyState(lobby);
    }
}
