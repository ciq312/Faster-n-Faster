using System.Security.Claims;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobby;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Helpers;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Results;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Infrastructure.Hubs;

public class GameHub : Hub
{
    private readonly ILogger<GameHub> _logger;
    private readonly ILobbyStore _lobbyStore;
    private readonly IHandler<JoinLobbyCommand> _joinHandler;
    private readonly ILobbyService _lobbyService;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly LobbyStateBroadcaster _broadcaster;

    public GameHub(
        ILogger<GameHub> logger,
        ILobbyStore lobbyStore,
        IHandler<JoinLobbyCommand> joinHandler,
        ILobbyService lobbyService,
        IHubContext<GameHub> hubContext,
        LobbyStateBroadcaster broadcaster
    )
    {
        _logger = logger;
        _lobbyStore = lobbyStore;
        _joinHandler = joinHandler;
        _lobbyService = lobbyService;
        _hubContext = hubContext;
        _broadcaster = broadcaster;
    }

    private (Guid UserId, Lobby Lobby, string GroupName) GetCallerContext()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new HubException("Not authenticated.");

        var userId = Guid.Parse(userIdClaim);

        var conn = _lobbyService.GetConnection(Context.ConnectionId)
            ?? throw new HubException("Not connected to a lobby.");

        var lobby = _lobbyStore.Get(conn.LobbyId)
            ?? throw new HubException("Lobby not found.");

        return (userId, lobby, $"lobby-{lobby.Id}");
    }

    public async Task ConnectToLobby(Guid lobbyId)
    {
        _logger.LogInformation($"connecting to {lobbyId}");
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
        {
            await Clients.Caller.SendAsync("Error", "Not authenticated.");
            return;
        }

        var userId = Guid.Parse(userIdClaim);
        var nick = Context.User!.FindFirst(ClaimTypes.Name)!.Value;

        try
        {
            var command = new JoinLobbyCommand(userId, lobbyId);

            await _joinHandler.Handle(command);

            _lobbyService.TrackConnection(Context.ConnectionId, lobbyId, userId);

            var lobby = _lobbyStore.Get(lobbyId)!;

            var groupName = $"lobby-{lobbyId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            await _broadcaster.BroadcastLobbyState(lobby);

            await Clients
                .OthersInGroup(groupName)
                .SendAsync("PlayerJoined", new { playerId = userId, displayName = nick });

            _logger.LogInformation(
                "Player {PlayerId} connected to lobby {LobbyId}",
                userId,
                lobbyId
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ConnectToLobby failed for player {PlayerId}", userId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task TransferHost(Guid targetPlayerId)
    {
        try
        {
            var (userId, lobby, groupName) = GetCallerContext();
            lobby.TransferHost(userId, targetPlayerId);

            await Clients.Group(groupName).SendAsync("HostChanged", new { newHostPlayerId = targetPlayerId });

            _logger.LogInformation(
                "Host transferred from {OldHost} to {NewHost} in lobby {LobbyId}",
                userId, targetPlayerId, lobby.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TransferHost failed for connection {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task StartRace()
    {
        try
        {
            var (userId, lobby, groupName) = GetCallerContext();
            lobby.StartRace(userId);

            await Clients.Group(groupName).SendAsync("RaceStarting", new { countdownSeconds = 3 });

            var hubContext = _hubContext;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(3000);
                    await hubContext.Clients.Group(groupName).SendAsync("RaceStarted",
                        new { words = lobby.Race!.Words });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to broadcast RaceStarted for lobby {LobbyId}", lobby.Id);
                }
            });

            _logger.LogInformation("Race started in lobby {LobbyId} by host {PlayerId}", lobby.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "StartRace failed for connection {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task ConfigureRace(string gameMode, int? wordCount, int? timerDuration)
    {
        try
        {
            var (userId, lobby, groupName) = GetCallerContext();

            Race race = gameMode.ToLowerInvariant() switch
            {
                "wordcount" => new WordRace(wordCount
                    ?? throw new HubException("wordCount is required for wordcount mode.")),
                "timer" => new TimerRace(timerDuration
                    ?? throw new HubException("timerDuration is required for timer mode.")),
                _ => throw new HubException($"Unknown game mode: {gameMode}")
            };

            lobby.ConfigureRace(userId, race);

            await Clients.Group(groupName).SendAsync("RaceConfigured", new { gameMode, wordCount, timerDuration });

            _logger.LogInformation("Race configured in lobby {LobbyId}: {GameMode}", lobby.Id, gameMode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ConfigureRace failed for connection {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task KickPlayer(Guid targetPlayerId)
    {
        try
        {
            var (userId, lobby, groupName) = GetCallerContext();
            var kicked = lobby.KickPlayer(userId, targetPlayerId);

            // Notify the kicked player and clean up their connection
            var kickedConnectionId = _lobbyService.GetConnectionId(lobby.Id, targetPlayerId);
            if (kickedConnectionId != null)
            {
                await Clients.Client(kickedConnectionId).SendAsync("Kicked");
                await Groups.RemoveFromGroupAsync(kickedConnectionId, groupName);
                _lobbyService.RemoveConnection(kickedConnectionId);
            }

            await Clients.Group(groupName).SendAsync("PlayerKicked", new { playerId = targetPlayerId });

            _logger.LogInformation(
                "Player {TargetId} kicked from lobby {LobbyId} by host {HostId}",
                targetPlayerId, lobby.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "KickPlayer failed for connection {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task UpdateProgress(int index, int totalTyped, int mistakes)
    {
        try
        {
            var (userId, lobby, groupName) = GetCallerContext();

            if (lobby.CurrentStatus != Lobby.Status.racing)
                throw new HubException("Race is not active.");

            var race = lobby.Race
                ?? throw new HubException("Race not configured.");

            var participant = race.ProcessUpdate(userId, index, totalTyped, mistakes);
            if (participant == null)
                return; // update rejected (finished or invalid)

            // Broadcast all players' positions in one message
            var players = race.Participants.Values.Select(p => new
            {
                playerId = p.PlayerId,
                index = p.Index
            });
            await Clients.Group(groupName).SendAsync("PlayersProgress", new { players });

            // Server-side finish detection
            if (participant.IsFinished)
            {
                await Clients.Group(groupName).SendAsync("PlayerFinished", new
                {
                    playerId = userId,
                    finishPosition = participant.FinishPosition,
                    wpm = participant.GetWpm(),
                    accuracy = participant.GetAccuracy()
                });

                _logger.LogInformation(
                    "Player {PlayerId} finished in position {Position} in lobby {LobbyId}",
                    userId, participant.FinishPosition, lobby.Id);
            }

            if (race.IsRaceOver())
            {
                lobby.TransitionStatus(Lobby.Status.finished);

                var results = race.Participants.Values
                    .OrderBy(p => p.FinishPosition)
                    .Select(p => new
                    {
                        playerId = p.PlayerId,
                        finishPosition = p.FinishPosition,
                        wpm = p.GetWpm(),
                        accuracy = p.GetAccuracy(),
                        mistakes = p.Mistakes
                    });

                await Clients.Group(groupName).SendAsync("RaceEnded", new { results });

                _logger.LogInformation("Race ended in lobby {LobbyId}", lobby.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UpdateProgress failed for connection {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var conn = _lobbyService.GetConnection(Context.ConnectionId);
        if (conn == null) throw new KeyNotFoundException("connection not found");
        var (lobbyId, playerId) = conn.Value;
        _lobbyService.RemoveConnection(Context.ConnectionId);

        var lobby = _lobbyStore.Get(lobbyId) ?? throw new KeyNotFoundException("lobby doesn't exist");

        var player = lobby.Players.FirstOrDefault(p => p.User.Id == playerId) ?? throw new UserNotFoundException($"player was not found in lobby {lobbyId}");

        player.Disconnect();

        var groupName = $"lobby-{lobbyId}";
        await Clients
            .Group(groupName)
            .SendAsync("PlayerDisconnected", new { playerId });

        // Host promotion: if host left, promote next connected player by join order
        if (lobby.HostId == playerId)
        {
            var nextHost = lobby
                .Players.Where(p => p.IsConnected && p.User.Id != playerId)
                .OrderBy(p => p.JoinOrder)
                .FirstOrDefault();

            lobby.AssignHost(nextHost!.User.Id);

            if (nextHost != null)
            {
                await Clients
                    .Group(groupName)
                    .SendAsync(
                        "HostChanged",
                        new { newHostPlayerId = nextHost.User.Id }
                    );
            }
        }

        _logger.LogInformation(
            "Player {PlayerId} disconnected from lobby {LobbyId}",
            playerId,
            lobbyId
        );
        await base.OnDisconnectedAsync(exception);
    }
}

