using FasterNFaster.Api.Core.Entities.Lobbies.Races.Events;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.Web.Hubs;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;

public class BroadcastRaceFinishedHandler(IHubContext<GameHub> hub, ILobbyStore lobbyStore,
    LobbyStateBroadcaster broadcaster,
    ILobbySessionService lobbySessionService) : IDomainEventHandler<RaceFinishedEvent>
{
    private readonly IHubContext<GameHub> _hub = hub;
    private readonly ILobbyStore _lobbyStore = lobbyStore;
    private readonly LobbyStateBroadcaster _broadcaster = broadcaster;
    private readonly ILobbySessionService lobbySessionService = lobbySessionService;

    public async Task Handle(RaceFinishedEvent e)
    {
        await _hub.Clients.Group(LobbyGroup(e.LobbyId)).SendAsync(Methods.RaceEnded, new
        {
            results = e.Results
        });

        var lobby = _lobbyStore.Get(e.LobbyId) ?? throw new LobbyNotFoundException(e.LobbyId);

        lobby.OnSessionEnded();

        await lobbySessionService.RefreshPassage(lobby.HostId);

        await _broadcaster.BroadcastLobbyState(lobby);
    }
}
