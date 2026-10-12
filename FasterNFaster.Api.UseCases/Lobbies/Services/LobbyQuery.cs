using FasterNFaster.Api.UseCases.Lobbies.LobbyState;
using FasterNFaster.Api.UseCases.Races.Ports;

namespace FasterNFaster.Api.UseCases.Lobbies.Services;

public class LobbyQuery(ILobbyAccess lobbies, IRaceStore races) : ILobbyQuery
{
    public async Task<LobbyStateDTO> GetLobbyState(Guid lobbyId)
    {
        var raceSettings = await races.Read(lobbyId, race => new RaceSettingsDTO(race.Passage, race.WordCount));

        return await lobbies.Read(lobbyId, lobby => new LobbyStateDTO(
            lobby.Id, lobby.Name, lobby.IsSessionActive, raceSettings, lobby.LobbySettings.IsPrivate,
            lobby.LobbySettings.InviteCode, lobby.LobbySettings.MaxPlayers,
            lobby.GetColors(),
            [.. lobby.Players.Select(p => new LobbyPlayerDTO(p.Id, lobby.IsPlayerHost(p.Id), p.Nick, p.JoinOrder, IsConnected: true, p.Color))]));
    }
}
