using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.Lobbies.LobbyState;

public class LobbyStateBroadcaster(IBroadcaster broadcaster, ILobbyStore lobbyStore, IRaceService raceService) : ILobbyStateBroadcaster
{
    private readonly IBroadcaster broadcaster = broadcaster;
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly IRaceService raceService = raceService;

    public async Task BroadcastLobbyState(Lobby lobby) =>
        await broadcaster.Broadcast(Audience.Lobby(lobby.Id), Methods.LobbyState, await GetLobbyState(lobby));

    public async Task BroadcastLobbyState(Guid lobbyId) =>
        await BroadcastLobbyState(lobbyStore.GetRequired(lobbyId));

    public async Task<LobbyStateDTO> GetLobbyState(Lobby lobby)
    {
        var players = lobby.Players.Select(p => new LobbyPlayerDto(
            p.User.Id, p.IsHost, p.User.Nick, p.JoinOrder, p.IsConnected, p.Color));

        var raceSettings = await raceService.GetRaceSettings(lobby.Id);

        return new LobbyStateDTO(
            lobby.Id, lobby.Name, raceSettings.RaceType, lobby.IsSessionActive, raceSettings, lobby.LobbySettings.IsPrivate,
            lobby.LobbySettings.InviteCode, lobby.LobbySettings.MaxPlayers,
            lobby.GetColors(), [.. players]);
    }
}
