using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.LobbyState;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyQuery(ILobbyAccess lobbies, IRaceService raceService) : ILobbyQuery
{
    public async Task<LobbyStateDTO> GetLobbyState(Guid lobbyId)
    {
        Lobby lobby = lobbies.GetRequired(lobbyId);

        var players = lobby.Players.Select(p => new LobbyPlayerDTO(p.Id, lobby.IsPlayerHost(p.Id), p.Nick, p.JoinOrder, IsConnected: true, p.Color));

        var raceSettings = await raceService.GetRaceSettings(lobbyId);

        return new LobbyStateDTO(
                 lobby.Id, lobby.Name, raceSettings.RaceType, lobby.IsSessionActive, raceSettings, lobby.LobbySettings.IsPrivate,
                 lobby.LobbySettings.InviteCode, lobby.LobbySettings.MaxPlayers,
                 lobby.GetColors(), [.. players]);
    }
}
