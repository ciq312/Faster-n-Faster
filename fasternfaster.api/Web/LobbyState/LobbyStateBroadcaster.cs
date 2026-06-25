using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.Lobbies.LobbyState;

public class LobbyStateBroadcaster(IHubContext<GameHub> hub, ILobbyStore lobbyStore, IRaceService raceService) : ILobbyStateBroadcaster
{
    private readonly IHubContext<GameHub> hub = hub;
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly IRaceService raceService = raceService;

    public Task BroadcastLobbyState(Lobby lobby)
    {
        var state = GetLobbyState(lobby);
        return hub.Clients.Group(LobbyGroup(lobby.Id)).SendAsync(Methods.LobbyState, state);
    }

    public Task BroadcastLobbyState(Guid lobbyId)
    {
        Lobby lobby = lobbyStore.GetRequired(lobbyId);
        return BroadcastLobbyState(lobby);
    }

    public LobbyStateDTO GetLobbyState(Lobby lobby)
    {
        var players = lobby.Players.Select(p => new LobbyPlayerDto(
            p.User.Id, p.IsHost, p.User.Nick, p.JoinOrder, p.IsConnected, p.Color));

        var raceSettings = raceService.GetRaceSettings(lobby.Id);

        return new LobbyStateDTO(
            lobby.Id, lobby.Name, raceSettings.RaceType, lobby.IsSessionActive, raceSettings, lobby.LobbySettings.IsPrivate,
            lobby.LobbySettings.InviteCode, lobby.LobbySettings.MaxPlayers,
            lobby.GetColors(), [.. players]);
    }
}
