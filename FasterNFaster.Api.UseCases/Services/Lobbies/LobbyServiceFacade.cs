using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.LobbyState;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyServiceFacade(ILobbyInternals lobbyInternals,
  IRaceInternals raceInternals,
  ILobbyService lobbyService,
  IRaceService raceService,
  IRaceTickRegistry raceTickRegistry
  ) : ILobbyServiceFacade, IRaceTransitionService
{
    public async Task StartSession(Guid hostId)
    {
        Lobby lobby = lobbyService.GetLobbyOfPlayerRequired(hostId);
        Guid lobbyId = lobby.Id;

        await lobbyInternals.ValidateHost(lobbyId, hostId);

        await lobbyInternals.StartSession(lobbyId, hostId);

        var participants = lobby.GetRaceParticipants();

        await raceInternals.AddParticipants(lobbyId, participants);

        raceTickRegistry.RegisterLobby(lobbyId);
    }
    public Task StartRaceInternal(Guid lobbyId) => raceInternals.StartRace(lobbyId);

    public Task UpdateProgress(Guid userId, int index, int mistakes, string typed)
    {
        Guid lobbyId = lobbyService.GetLobbyIdOfPlayerRequired(userId);
        return raceService.ProcessUpdate(lobbyId, userId, index, mistakes, typed);
    }

    public Task EndSession(Guid lobbyId) => lobbyInternals.EndSession(lobbyId);

    public async Task RemoveLobbyIfEmpty(Guid lobbyId)
    {
        Lobby lobby = lobbyService.GetLobbyRequired(lobbyId);

        if (lobby.IsEmpty())
        {
            await lobbyInternals.RemoveLobby(lobbyId);

            raceService.RemoveRegisteredRace(lobbyId);

            raceTickRegistry.DeregisterLobby(lobbyId);
        }
    }
    public async Task KickPlayer(Guid hostId, Guid userId)
    {
        Lobby lobby = lobbyService.GetLobbyOfPlayerRequired(userId);

        await lobbyInternals.KickPlayer(hostId, userId);

        if (lobby.IsSessionActive)
            await raceInternals.WithdrawParticipant(lobby.Id, userId);
    }

    public async Task RefreshPassage(Guid userId)
    {
        Lobby lobby = lobbyService.GetLobbyOfPlayerRequired(userId);

        if (lobby.IsSessionActive) throw new InvalidOperationException("Can't refresh when session active");

        await lobbyInternals.ValidateHost(lobby.Id, userId);

        await raceInternals.RefreshPassage(lobby.Id);
    }

    public async Task RemovePlayerFromLobby(Guid userId)
    {
        Lobby lobby = lobbyService.GetLobbyOfPlayerRequired(userId);

        await lobbyInternals.RemoveFromLobby(userId);

        if (lobby.IsSessionActive)
            await raceInternals.WithdrawParticipant(lobby.Id, userId);
    }

    public async Task<LobbyStateDTO> GetLobbyStateDTO(Guid lobbyId)
    {
        Lobby lobby = lobbyService.GetLobbyRequired(lobbyId);

        var players = lobby.Players.Select(p => new LobbyPlayerDTO(p.Id, lobby.IsPlayerHost(p.Id), p.Nick, p.JoinOrder, IsConnected: true, p.Color));

        var raceSettings = await raceService.GetRaceSettings(lobbyId);

        return new LobbyStateDTO(
                 lobby.Id, lobby.Name, raceSettings.RaceType, lobby.IsSessionActive, raceSettings, lobby.LobbySettings.IsPrivate,
                 lobby.LobbySettings.InviteCode, lobby.LobbySettings.MaxPlayers,
                 lobby.GetColors(), [.. players]);
    }

    public Task<bool> DoesLobbyExist(Guid lobbyId)
    {
        return lobbyService.DoesLobbyExist(lobbyId);
    }
}