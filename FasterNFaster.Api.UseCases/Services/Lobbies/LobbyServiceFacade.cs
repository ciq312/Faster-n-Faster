using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.LobbyState;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyServiceFacade(ILobbyAccess lobbies,
  IRaceInternals raceInternals,
  IRaceService raceService,
  IRaceTickRegistry raceTickRegistry
  ) : ILobbyServiceFacade, IRaceTransitionService
{
    public Task StartRaceInternal(Guid lobbyId) => raceInternals.StartRace(lobbyId);

    public Task UpdateProgress(Guid userId, int index, int mistakes, string typed)
    {
        Guid lobbyId = lobbies.GetLobbyIdOfPlayerRequired(userId);
        return raceService.ProcessUpdate(lobbyId, userId, index, mistakes, typed);
    }

    public Task EndSession(Guid lobbyId) => lobbies.Mutate(lobbyId, l => l.EndSession());

    public async Task RemoveLobbyIfEmpty(Guid lobbyId)
    {
        Lobby lobby = lobbies.GetRequired(lobbyId);

        if (lobby.IsEmpty())
        {
            await lobbies.Remove(lobbyId);

            raceService.RemoveRegisteredRace(lobbyId);

            raceTickRegistry.DeregisterLobby(lobbyId);
        }
    }

    public async Task KickPlayer(Guid hostId, Guid userId)
    {
        Lobby lobby = lobbies.GetOfPlayerRequired(userId);

        await lobbies.Mutate(lobby.Id, l => l.Kick(hostId, userId));

        if (lobby.IsSessionActive)
            await raceInternals.WithdrawParticipant(lobby.Id, userId);
    }

    public async Task RefreshPassage(Guid userId)
    {
        Lobby lobby = lobbies.GetOfPlayerRequired(userId);

        if (lobby.IsSessionActive) throw new InvalidOperationException("Can't refresh when session active");

        lobby.ValidateHost(userId);

        await raceInternals.RefreshPassage(lobby.Id);
    }

    public async Task RemovePlayerFromLobby(Guid userId)
    {
        Lobby lobby = lobbies.GetOfPlayerRequired(userId);

        await lobbies.Mutate(lobby.Id, l => l.Disconnect(userId));

        if (lobby.IsSessionActive)
            await raceInternals.WithdrawParticipant(lobby.Id, userId);
    }

    public async Task<LobbyStateDTO> GetLobbyStateDTO(Guid lobbyId)
    {
        Lobby lobby = lobbies.GetRequired(lobbyId);

        var players = lobby.Players.Select(p => new LobbyPlayerDTO(p.Id, lobby.IsPlayerHost(p.Id), p.Nick, p.JoinOrder, IsConnected: true, p.Color));

        var raceSettings = await raceService.GetRaceSettings(lobbyId);

        return new LobbyStateDTO(
                 lobby.Id, lobby.Name, raceSettings.RaceType, lobby.IsSessionActive, raceSettings, lobby.LobbySettings.IsPrivate,
                 lobby.LobbySettings.InviteCode, lobby.LobbySettings.MaxPlayers,
                 lobby.GetColors(), [.. players]);
    }

    public bool DoesLobbyExist(Guid lobbyId) => lobbies.Exists(lobbyId);
}
