using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbySessionService(ILobbyCoordinator lobbyCoordinator,
  IRaceCoordinator raceCoordinator,
  ILobbyService lobbyService,
  IRaceService raceService,
  IRaceTickRegistry raceTickRegistry
  ) : ILobbySessionService, IRaceTransitionService
{
    private readonly IRaceTickRegistry raceTickRegistry = raceTickRegistry;
    private readonly ILobbyCoordinator lobbyCoordinator = lobbyCoordinator;
    private readonly IRaceCoordinator raceCoordinator = raceCoordinator;
    private readonly ILobbyService lobbyService = lobbyService;
    private readonly IRaceService raceService = raceService;

    public async Task StartSession(Guid hostId)
    {
        Lobby lobby = lobbyService.GetLobbyOfPlayerRequired(hostId);
        Guid lobbyId = lobby.Id;

        await lobbyCoordinator.ValidateHost(lobbyId, hostId);

        await lobbyCoordinator.StartSession(lobbyId, hostId);

        var participants = lobby.GetRaceParticipants();

        await raceCoordinator.AddParticipants(lobbyId, participants);

        raceTickRegistry.RegisterLobby(lobbyId);
    }
    public Task StartRaceInternal(Guid lobbyId) => raceCoordinator.StartRace(lobbyId);

    public async Task RemoveLobbyIfEmpty(Guid lobbyId)
    {
        Lobby lobby = lobbyService.GetLobbyRequired(lobbyId);

        if (lobby.IsEmpty())
        {
            await lobbyCoordinator.RemoveLobby(lobbyId);

            raceService.RemoveRegisteredRace(lobbyId);

            raceTickRegistry.DeregisterLobby(lobbyId);
        }
    }
    public async Task KickPlayer(Guid hostId, Guid userId)
    {
        Lobby lobby = lobbyService.GetLobbyOfPlayerRequired(userId);

        await lobbyCoordinator.KickPlayer(hostId, userId);

        if (lobby.IsSessionActive)
            await raceCoordinator.WithdrawParticipant(lobby.Id, userId);
    }

    public async Task RefreshPassage(Guid userId)
    {
        Lobby lobby = lobbyService.GetLobbyOfPlayerRequired(userId);

        if (lobby.IsSessionActive) throw new InvalidOperationException("Can't refresh when session active");

        await lobbyCoordinator.ValidateHost(lobby.Id, userId);

        await raceCoordinator.RefreshPassage(lobby.Id);
    }

    public async Task RemovePlayerFromLobby(Guid userId)
    {
        Lobby lobby = lobbyService.GetLobbyOfPlayerRequired(userId);

        await lobbyCoordinator.RemoveFromLobby(userId);

        if (lobby.IsSessionActive)
            await raceCoordinator.WithdrawParticipant(lobby.Id, userId);
    }
}