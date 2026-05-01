using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Lobbies.LobbyState;

public class LobbyStateBroadcaster(IHubContext<GameHub> hub, ILobbyStore lobbyStore)
{
    private readonly IHubContext<GameHub> hub = hub;
    private readonly ILobbyStore lobbyStore = lobbyStore;

    public async Task BroadcastLobbyState(Lobby lobby)
    {
        var state = GetLobbyState(lobby);
        await hub.Clients.Group($"lobby-{lobby.Id}")
        .SendAsync("LobbyState", state);
    }

    public async Task BroadcastLobbyState(Guid lobbyId)
    {
        Lobby lobby = lobbyStore.GetRequired(lobbyId);
        await BroadcastLobbyState(lobby);
    }

    public object GetLobbyState(Lobby lobby)
    {
        var players = lobby.Players.Select(p => new LobbyPlayerDto(
            p.User.Id, p.IsHost, p.User.Nick, p.JoinOrder, p.IsConnected, p.Color));

        var race = lobby.Race;

        return new LobbyStateDTO(
            lobby.Id, lobby.Name, race.GetType().Name, lobby.IsSessionActive, race.GetRaceSettings(), lobby.LobbySettings.IsPrivate,
            lobby.LobbySettings.InviteCode, lobby.LobbySettings.MaxPlayers,
            lobby.GetColors(), [.. players]);
    }
}