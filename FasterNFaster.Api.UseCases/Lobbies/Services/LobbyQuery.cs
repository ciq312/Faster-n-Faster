using FasterNFaster.Api.UseCases.Lobbies.LobbyState;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Races.Ports;

namespace FasterNFaster.Api.UseCases.Lobbies.Services;

public class LobbyQuery(ILobbyStore lobbies, IRaceStore races) : ILobbyQuery
{
    public async Task<LobbyStateDTO> GetLobbyState(Guid lobbyId)
    {
        var (raceSettings, raceHasStarted) = await races.Read(lobbyId, race =>
            (new RaceSettingsDTO(race.Passage, race.WordCount), race.HasStarted));

        return await lobbies.Read(lobbyId, lobby => new LobbyStateDTO(
            lobby.Id, lobby.Name,
            IsRaceStarting: lobby.IsSessionActive && !raceHasStarted,
            IsRaceStarted: lobby.IsSessionActive && raceHasStarted,
            raceSettings, lobby.LobbySettings.IsPrivate,
            lobby.LobbySettings.InviteCode, lobby.LobbySettings.MaxPlayers,
            [.. lobby.GetColors().Select(c => new ColorStatusDTO(c.Color, c.IsAvailable))],
            [.. lobby.Players.Select(p => new LobbyPlayerDTO(p.Id, lobby.IsPlayerHost(p.Id), p.Nick, p.JoinOrder, IsConnected: true, p.Color))]));
    }
}
