using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.LobbyState;
using FasterNFaster.Api.UseCases.Races.Services;

namespace FasterNFaster.Api.UseCases.Services.Lobbies;

public class LobbyQuery(ILobbyAccess lobbies, IRaceAccess races) : ILobbyQuery
{
    public async Task<LobbyStateDTO> GetLobbyState(Guid lobbyId)
    {
        var raceSettings = await races.GetRaceSettings(lobbyId);

        return await lobbies.Read(lobbyId, lobby => new LobbyStateDTO(
            lobby.Id, lobby.Name, raceSettings.RaceType, lobby.IsSessionActive, raceSettings, lobby.LobbySettings.IsPrivate,
            lobby.LobbySettings.InviteCode, lobby.LobbySettings.MaxPlayers,
            lobby.GetColors(),
            [.. lobby.Players.Select(p => new LobbyPlayerDTO(p.Id, lobby.IsPlayerHost(p.Id), p.Nick, p.JoinOrder, IsConnected: true, p.Color))]));
    }
}
