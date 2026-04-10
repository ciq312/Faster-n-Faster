using System.Security.Claims;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using FasterNFaster.Api.UseCases.Lobbies.FastReconnect;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Results;
using FasterNFaster.Api.UseCases.Lobbies.KickPlayer;
using FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;
using FasterNFaster.Api.UseCases.Lobbies.StartRace;
using FasterNFaster.Api.UseCases.Lobbies.TransferHost;
using FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Infrastructure.Hubs;

public class GameHub(
    ILogger<GameHub> logger,
    ILobbyStore lobbyStore,
    ILobbyService lobbyService,
    LobbyStateBroadcaster broadcaster,
    IHandler<JoinLobbyCommand, JoinLobbyResult> joinHandler,
    IHandler<StartRaceCommand> startRaceHandler,
    IHandler<TransferHostCommand> transferHostHandler,
    IHandler<KickPlayerCommand, KickPlayerResult> kickPlayerHandler,
    IHandler<UpdateProgressCommand> updateProgressHandler,
    IHandler<DisconnectCommand, DisconnectResult> disconnectHandler,
    IHandler<FastReconnectCommand> fastReconnectHandler,
    IHandler<RefreshPassageCommand> refreshPassageHandler) : Hub
{
    private readonly ILogger<GameHub> logger = logger;
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly ILobbyService lobbyService = lobbyService;
    private readonly LobbyStateBroadcaster broadcaster = broadcaster;

    private readonly IHandler<JoinLobbyCommand, JoinLobbyResult> joinHandler = joinHandler;
    private readonly IHandler<StartRaceCommand> startRaceHandler = startRaceHandler;
    private readonly IHandler<TransferHostCommand> transferHostHandler = transferHostHandler;
    private readonly IHandler<KickPlayerCommand, KickPlayerResult> kickPlayerHandler = kickPlayerHandler;
    private readonly IHandler<UpdateProgressCommand> updateProgressHandler = updateProgressHandler;
    private readonly IHandler<DisconnectCommand, DisconnectResult> disconnectHandler = disconnectHandler;
    private readonly IHandler<FastReconnectCommand> fastReconnectHandler = fastReconnectHandler;
    private readonly IHandler<RefreshPassageCommand> _refreshPassageHandler = refreshPassageHandler;

    private (Guid UserId, Guid LobbyId, string GroupName) GetCallerContext()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new HubException("Not authenticated.");

        var userId = Guid.Parse(userIdClaim);

        var conn = lobbyService.GetConnection(Context.ConnectionId)
            ?? throw new HubException("Not connected to a lobby.");

        return (userId, conn.LobbyId, $"lobby-{conn.LobbyId}");
    }

    public async Task ConnectToLobby(Guid lobbyId, string? inviteCode = null)
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
            var result = await joinHandler.Handle(new JoinLobbyCommand(userId, lobbyId, inviteCode));
            var groupName = $"lobby-{lobbyId}";

            lobbyService.TrackConnection(Context.ConnectionId, lobbyId, userId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var lobby = lobbyStore.Get(lobbyId)!;
            await broadcaster.BroadcastLobbyState(lobby);

            if (result.IsReconnect)
            {
                logger.LogInformation("Player {PlayerId} reconnected to lobby {LobbyId}", userId, lobbyId);
                return;
            }

            await Clients.OthersInGroup(groupName)
                .SendAsync("PlayerJoined", new { playerId = userId, displayName = nick });

            logger.LogInformation("Player {PlayerId} connected to lobby {LobbyId}", userId, lobbyId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ConnectToLobby failed for player {PlayerId}", userId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task StartRace()
    {
        try
        {
            var (userId, lobbyId, groupName) = GetCallerContext();


            await startRaceHandler.Handle(new StartRaceCommand(userId, lobbyId));

            await Clients.Group(groupName).SendAsync("RaceStarting", new
            {
                countdownSeconds = 3,
            });

            logger.LogInformation("Race starting in lobby {LobbyId} by host {PlayerId}", lobbyId, userId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "StartRace failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task ChangeGameMode(string gameMode)
    {
        throw new NotImplementedException();
        // try
        // {
        //     var (userId, lobbyId, _) = GetCallerContext();
        //     var lobby = lobbyStore.Get(lobbyId) ?? throw new HubException("Lobby not found.");
        //     // lobby.UpdateRaceSettings(userId, s => s.SetGameMode(gameMode));
        //     await broadcaster.BroadcastLobbyState(lobby);
        // }
        // catch (Exception ex)
        // {
        //     logger.LogWarning(ex, "ChangeGameMode failed");
        //     await Clients.Caller.SendAsync("Error", ex.Message);
        // }
    }

    public async Task ChangeWordCount(int wordCount)
    {
        throw new NotImplementedException();
        // try
        // {
        //     var (userId, lobbyId, _) = GetCallerContext();
        //     var lobby = lobbyStore.Get(lobbyId) ?? throw new HubException("Lobby not found.");
        //     // lobby.UpdateRaceSettings(userId, s => s.SetWordCount(wordCount));
        //     await broadcaster.BroadcastLobbyState(lobby);
        // }
        // catch (Exception ex)
        // {
        //     logger.LogWarning(ex, "ChangeWordCount failed");
        //     await Clients.Caller.SendAsync("Error", ex.Message);
        // }
    }

    public async Task ChangeTimerDuration(int duration)
    {
        throw new NotImplementedException();
        // try
        // {
        //     var (userId, lobbyId, _) = GetCallerContext();
        //     var lobby = lobbyStore.Get(lobbyId) ?? throw new HubException("Lobby not found.");
        //     // lobby.UpdateRaceSettings(userId, s => s.SetTimerDuration(duration));
        //     await broadcaster.BroadcastLobbyState(lobby);
        // }
        // catch (Exception ex)
        // {
        //     logger.LogWarning(ex, "ChangeTimerDuration failed");
        //     await Clients.Caller.SendAsync("Error", ex.Message);
        // }
    }

    public async Task RefreshPassage()
    {
        try
        {
            var (userId, lobbyId, _) = GetCallerContext();
            await _refreshPassageHandler.Handle(new RefreshPassageCommand(userId, lobbyId));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RefreshPassage failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task TransferHost(Guid targetPlayerId)
    {
        try
        {
            var (userId, lobbyId, groupName) = GetCallerContext();

            await transferHostHandler.Handle(new TransferHostCommand(userId, lobbyId, targetPlayerId));

            await Clients.Group(groupName).SendAsync("HostChanged", new { newHostPlayerId = targetPlayerId });

            logger.LogInformation("Host transferred from {OldHost} to {NewHost} in lobby {LobbyId}",
                userId, targetPlayerId, lobbyId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TransferHost failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task KickPlayer(Guid targetPlayerId)
    {
        try
        {
            var (userId, lobbyId, groupName) = GetCallerContext();

            var result = await kickPlayerHandler.Handle(
                new KickPlayerCommand(userId, lobbyId, targetPlayerId));

            if (result.KickedConnectionId != null)
            {
                await Clients.Client(result.KickedConnectionId).SendAsync("Kicked");
                await Groups.RemoveFromGroupAsync(result.KickedConnectionId, groupName);
            }

            await Clients.Group(groupName).SendAsync("PlayerKicked", new { playerId = result.TargetPlayerId });

            logger.LogInformation("Player {TargetId} kicked from lobby {LobbyId}", targetPlayerId, lobbyId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "KickPlayer failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task ChangeColor(string color)
    {
        try
        {
            var (userId, lobbyId, groupName) = GetCallerContext();
            var lobby = lobbyStore.Get(lobbyId)
                ?? throw new HubException("Lobby not found.");

            Log.Information($"chaning color to {color}");
            lobby.ChangePlayerColor(userId, color);
            await broadcaster.BroadcastLobbyState(lobby);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ChangeColor failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task UpdateRaceState(int index, int mistakes)
    {
        try
        {
            var (userId, lobbyId, _) = GetCallerContext();
            await updateProgressHandler.Handle(
                new UpdateProgressCommand(userId, lobbyId, index, mistakes));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "UpdateRaceState failed");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task LeaveLobby()
    {
        var conn = lobbyService.GetConnection(Context.ConnectionId);
        if (conn == null)
        {
            await base.OnDisconnectedAsync(null);
            return;
        }

        var (lobbyId, playerId) = conn.Value;
        var groupName = $"lobby-{lobbyId}";

        try
        {
            var result = await disconnectHandler.Handle(
                new DisconnectCommand(Context.ConnectionId, lobbyId, playerId));

            await Clients.Group(groupName).SendAsync("PlayerDisconnected", new { playerId = result.PlayerId });

            if (result.NewHostId != null)
            {
                await Clients.Group(groupName)
                    .SendAsync("HostChanged", new { newHostPlayerId = result.NewHostId });
            }

            var lobby = lobbyStore.Get(lobbyId);
            if (lobby != null)
                await broadcaster.BroadcastLobbyState(lobby);

            logger.LogInformation("Player {PlayerId} disconnected from lobby {LobbyId}", playerId, lobbyId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LeaveLobby failed for {ConnectionId}", Context.ConnectionId);
        }
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var conn = lobbyService.GetConnection(Context.ConnectionId);
        if (conn == null)
        {
            await base.OnDisconnectedAsync(exception);
            return;
        }

        var (lobbyId, playerId) = conn.Value;
        var groupName = $"lobby-{lobbyId}";

        try
        {
            await fastReconnectHandler.Handle(new FastReconnectCommand(playerId, lobbyId));
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
            var result = await disconnectHandler.Handle(
                new DisconnectCommand(Context.ConnectionId, lobbyId, playerId));

            await Clients.Group(groupName).SendAsync("PlayerDisconnected", new { playerId = result.PlayerId });

            if (result.NewHostId != null)
            {
                await Clients.Group(groupName)
                    .SendAsync("HostChanged", new { newHostPlayerId = result.NewHostId });
            }

            var lobby = lobbyStore.Get(lobbyId);
            if (lobby != null)
                await broadcaster.BroadcastLobbyState(lobby);

            logger.LogInformation("Player {PlayerId} disconnected from lobby {LobbyId}", playerId, lobbyId);


            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OnDisconnectedAsync failed for {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
