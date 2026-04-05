using System.Security.Claims;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using FasterNFaster.Api.UseCases.Lobbies.FastReconnect;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Results;
using FasterNFaster.Api.UseCases.Lobbies.KickPlayer;
using FasterNFaster.Api.UseCases.Lobbies.StartRace;
using FasterNFaster.Api.UseCases.Lobbies.TransferHost;
using FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Infrastructure.Hubs;

public class GameHub : Hub
{
    private readonly ILogger<GameHub> _logger;
    private readonly ILobbyStore _lobbyStore;
    private readonly ILobbyService _lobbyService;
    private readonly LobbyStateBroadcaster _broadcaster;

    private readonly IHandler<JoinLobbyCommand, JoinLobbyResult> _joinHandler;
    private readonly IHandler<StartRaceCommand, StartRaceResult> _startRaceHandler;
    private readonly IHandler<TransferHostCommand> _transferHostHandler;
    private readonly IHandler<KickPlayerCommand, KickPlayerResult> _kickPlayerHandler;
    private readonly IHandler<UpdateProgressCommand> _updateProgressHandler;
    private readonly IHandler<DisconnectCommand, DisconnectResult> _disconnectHandler;
    private readonly IHandler<FastReconnectCommand> _fastReconnectHandler;
    private readonly IPassageProvider _passageProvider;

    public GameHub(
        ILogger<GameHub> logger,
        ILobbyStore lobbyStore,
        ILobbyService lobbyService,
        LobbyStateBroadcaster broadcaster,
        IPassageProvider passageProvider,
        IHandler<JoinLobbyCommand, JoinLobbyResult> joinHandler,
        IHandler<StartRaceCommand, StartRaceResult> startRaceHandler,
        IHandler<TransferHostCommand> transferHostHandler,
        IHandler<KickPlayerCommand, KickPlayerResult> kickPlayerHandler,
        IHandler<UpdateProgressCommand> updateProgressHandler,
        IHandler<DisconnectCommand, DisconnectResult> disconnectHandler,
        IHandler<FastReconnectCommand> fastReconnectHandler)
    {
        _logger = logger;
        _lobbyStore = lobbyStore;
        _lobbyService = lobbyService;
        _broadcaster = broadcaster;
        _passageProvider = passageProvider;
        _joinHandler = joinHandler;
        _startRaceHandler = startRaceHandler;
        _transferHostHandler = transferHostHandler;
        _kickPlayerHandler = kickPlayerHandler;
        _updateProgressHandler = updateProgressHandler;
        _disconnectHandler = disconnectHandler;
        _fastReconnectHandler = fastReconnectHandler;
    }

    private (Guid UserId, Guid LobbyId, string GroupName) GetCallerContext()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new HubException("Not authenticated.");

        var userId = Guid.Parse(userIdClaim);

        var conn = _lobbyService.GetConnection(Context.ConnectionId)
            ?? throw new HubException("Not connected to a lobby.");

        return (userId, conn.LobbyId, $"lobby-{conn.LobbyId}");
    }

    public async Task ConnectToLobby(Guid lobbyId)
    {
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
            var result = await _joinHandler.Handle(new JoinLobbyCommand(userId, lobbyId));
            var groupName = $"lobby-{lobbyId}";

            _lobbyService.TrackConnection(Context.ConnectionId, lobbyId, userId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var lobby = _lobbyStore.Get(lobbyId)!;
            await _broadcaster.BroadcastLobbyState(lobby);

            if (result.IsReconnect)
            {
                _logger.LogInformation("Player {PlayerId} reconnected to lobby {LobbyId}", userId, lobbyId);
                return;
            }

            await Clients.OthersInGroup(groupName)
                .SendAsync("PlayerJoined", new { playerId = userId, displayName = nick });

            _logger.LogInformation("Player {PlayerId} connected to lobby {LobbyId}", userId, lobbyId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ConnectToLobby failed for player {PlayerId}", userId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task StartRace()
    {
        try
        {
            var (userId, lobbyId, groupName) = GetCallerContext();


            var result = await _startRaceHandler.Handle(new StartRaceCommand(userId, lobbyId));

            await Clients.Group(groupName).SendAsync("RaceStarting", new
            {
                countdownSeconds = 3,
                words = result.Words
            });

            _logger.LogInformation("Race starting in lobby {LobbyId} by host {PlayerId}", lobbyId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "StartRace failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task ChangeGameMode(string gameMode)
    {
        try
        {
            var (userId, lobbyId, _) = GetCallerContext();
            var lobby = _lobbyStore.Get(lobbyId) ?? throw new HubException("Lobby not found.");
            lobby.UpdateRaceSettings(userId, s => s.SetGameMode(gameMode));
            await _broadcaster.BroadcastLobbyState(lobby);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ChangeGameMode failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task ChangeWordCount(int wordCount)
    {
        try
        {
            var (userId, lobbyId, _) = GetCallerContext();
            var lobby = _lobbyStore.Get(lobbyId) ?? throw new HubException("Lobby not found.");
            lobby.UpdateRaceSettings(userId, s => s.SetWordCount(wordCount));
            await _broadcaster.BroadcastLobbyState(lobby);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ChangeWordCount failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task ChangeTimerDuration(int duration)
    {
        try
        {
            var (userId, lobbyId, _) = GetCallerContext();
            var lobby = _lobbyStore.Get(lobbyId) ?? throw new HubException("Lobby not found.");
            lobby.UpdateRaceSettings(userId, s => s.SetTimerDuration(duration));
            await _broadcaster.BroadcastLobbyState(lobby);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ChangeTimerDuration failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task RefreshPassage()
    {
        try
        {
            var (userId, lobbyId, _) = GetCallerContext();
            var lobby = _lobbyStore.Get(lobbyId) ?? throw new HubException("Lobby not found.");
            var passage = await _passageProvider.GetPassageAsync(lobby.RaceSettings.WordCount);
            lobby.RefreshPassage(userId, passage);
            await _broadcaster.BroadcastLobbyState(lobby);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RefreshPassage failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task TransferHost(Guid targetPlayerId)
    {
        try
        {
            var (userId, lobbyId, groupName) = GetCallerContext();

            await _transferHostHandler.Handle(new TransferHostCommand(userId, lobbyId, targetPlayerId));

            await Clients.Group(groupName).SendAsync("HostChanged", new { newHostPlayerId = targetPlayerId });

            _logger.LogInformation("Host transferred from {OldHost} to {NewHost} in lobby {LobbyId}",
                userId, targetPlayerId, lobbyId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TransferHost failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task KickPlayer(Guid targetPlayerId)
    {
        try
        {
            var (userId, lobbyId, groupName) = GetCallerContext();

            var result = await _kickPlayerHandler.Handle(
                new KickPlayerCommand(userId, lobbyId, targetPlayerId));

            if (result.KickedConnectionId != null)
            {
                await Clients.Client(result.KickedConnectionId).SendAsync("Kicked");
                await Groups.RemoveFromGroupAsync(result.KickedConnectionId, groupName);
            }

            await Clients.Group(groupName).SendAsync("PlayerKicked", new { playerId = result.TargetPlayerId });

            _logger.LogInformation("Player {TargetId} kicked from lobby {LobbyId}", targetPlayerId, lobbyId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "KickPlayer failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task ChangeColor(string color)
    {
        try
        {
            var (userId, lobbyId, groupName) = GetCallerContext();
            var lobby = _lobbyStore.Get(lobbyId)
                ?? throw new HubException("Lobby not found.");

            Log.Information($"chaning color to {color}");
            lobby.ChangePlayerColor(userId, color);
            await _broadcaster.BroadcastLobbyState(lobby);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ChangeColor failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task UpdateRaceState(int index, int mistakes)
    {
        try
        {
            var (userId, lobbyId, _) = GetCallerContext();
            await _updateProgressHandler.Handle(
                new UpdateProgressCommand(userId, lobbyId, index, mistakes));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UpdateRaceState failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task LeaveLobby()
    {
        var conn = _lobbyService.GetConnection(Context.ConnectionId);
        if (conn == null)
        {
            await base.OnDisconnectedAsync(null);
            return;
        }

        var (lobbyId, playerId) = conn.Value;
        var groupName = $"lobby-{lobbyId}";

        try
        {
            var result = await _disconnectHandler.Handle(
                new DisconnectCommand(Context.ConnectionId, lobbyId, playerId));

            await Clients.Group(groupName).SendAsync("PlayerDisconnected", new { playerId = result.PlayerId });

            if (result.NewHostId != null)
            {
                await Clients.Group(groupName)
                    .SendAsync("HostChanged", new { newHostPlayerId = result.NewHostId });
            }

            var lobby = _lobbyStore.Get(lobbyId);
            if (lobby != null)
                await _broadcaster.BroadcastLobbyState(lobby);

            _logger.LogInformation("Player {PlayerId} disconnected from lobby {LobbyId}", playerId, lobbyId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LeaveLobby failed for {ConnectionId}", Context.ConnectionId);
        }
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var conn = _lobbyService.GetConnection(Context.ConnectionId);
        if (conn == null)
        {
            await base.OnDisconnectedAsync(exception);
            return;
        }

        var (lobbyId, playerId) = conn.Value;
        var groupName = $"lobby-{lobbyId}";

        try
        {
            await _fastReconnectHandler.Handle(new FastReconnectCommand(playerId, lobbyId));
        }
        catch (OperationCanceledException)
        {
            Log.Information("Fast reconnect for player happened");
            return;
        }
        catch (Exception e)
        {
            Log.Information(e.Message);
        }
        try
        {
            var result = await _disconnectHandler.Handle(
                new DisconnectCommand(Context.ConnectionId, lobbyId, playerId));

            await Clients.Group(groupName).SendAsync("PlayerDisconnected", new { playerId = result.PlayerId });

            if (result.NewHostId != null)
            {
                await Clients.Group(groupName)
                    .SendAsync("HostChanged", new { newHostPlayerId = result.NewHostId });
            }

            var lobby = _lobbyStore.Get(lobbyId);
            if (lobby != null)
                await _broadcaster.BroadcastLobbyState(lobby);

            _logger.LogInformation("Player {PlayerId} disconnected from lobby {LobbyId}", playerId, lobbyId);


            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OnDisconnectedAsync failed for {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
