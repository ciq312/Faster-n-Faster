using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

public class LobbyStateBroadcaster
{
    private readonly IHubContext<GameHub> _hub;

    public LobbyStateBroadcaster(IHubContext<GameHub> hub)
    {
        _hub = hub;
    }

    public async Task BroadcastLobbyState(Lobby lobby)
    {
        var state = GetLobbyState(lobby);
        await _hub.Clients.Group($"lobby-{lobby.Id}")
        .SendAsync("LobbyState", state);
    }

    public object GetLobbyState(Lobby lobby)
    {
        var players = lobby.Players.Select(p => new LobbyPlayerDto(
            p.User.Id, p.IsHost, p.User.Nick, p.JoinOrder, p.IsConnected, p.Color));

        var race = lobby.Race;
        var raceSettingsDto = new RaceSettingsDto(race.GetType(), race.GetRaceSettings());

        return new LobbyStateDTO(
            lobby.Id, lobby.Name, raceSettingsDto, lobby.LobbySettings.IsPrivate,
            lobby.LobbySettings.InviteCode, lobby.LobbySettings.MaxPlayers,
            lobby.GetColors(), [.. players]);
    }
}