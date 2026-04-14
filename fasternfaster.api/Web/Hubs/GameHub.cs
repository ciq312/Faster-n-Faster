using System.Security.Claims;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Exceptions;
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
    private readonly IHandler<RefreshPassageCommand> refreshPassageHandler = refreshPassageHandler;

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
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new HubException("Not authenticated.");

        var userId = Guid.Parse(userIdClaim);
        var nick = Context.User!.FindFirst(ClaimTypes.Name)!.Value;
        var groupName = $"lobby-{lobbyId}";

        var result = await joinHandler.Handle(new JoinLobbyCommand(userId, lobbyId, inviteCode));

        lobbyService.TrackConnection(Context.ConnectionId, lobbyId, userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var lobby = lobbyStore.Get(lobbyId) ?? throw new LobbyNotFoundException(lobbyId);
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

    public async Task StartRace()
    {
        var (userId, lobbyId, groupName) = GetCallerContext();

        await startRaceHandler.Handle(new StartRaceCommand(userId, lobbyId));

        await Clients.Group(groupName).SendAsync("RaceStarting", new { countdownSeconds = 3 });

        logger.LogInformation("Race starting in lobby {LobbyId} by host {PlayerId}", lobbyId, userId);
    }

    public async Task RefreshPassage()
    {
        var (userId, lobbyId, _) = GetCallerContext();
        await refreshPassageHandler.Handle(new RefreshPassageCommand(userId, lobbyId));
    }

    public async Task TransferHost(Guid targetPlayerId)
    {
        var (userId, lobbyId, _) = GetCallerContext();

        await transferHostHandler.Handle(new TransferHostCommand(userId, lobbyId, targetPlayerId));

        var lobby = lobbyStore.Get(lobbyId);
        if (lobby != null)
            await broadcaster.BroadcastLobbyState(lobby);

        logger.LogInformation("Host transferred from {OldHost} to {NewHost} in lobby {LobbyId}",
            userId, targetPlayerId, lobbyId);
    }

    public async Task KickPlayer(Guid targetPlayerId)
    {
        var (userId, lobbyId, groupName) = GetCallerContext();

        var result = await kickPlayerHandler.Handle(new KickPlayerCommand(userId, lobbyId, targetPlayerId));

        if (result.KickedConnectionId != null)
        {
            await Clients.Client(result.KickedConnectionId).SendAsync("Kicked");
            await Groups.RemoveFromGroupAsync(result.KickedConnectionId, groupName);
        }

        await Clients.Group(groupName).SendAsync("PlayerKicked", new { playerId = result.TargetPlayerId });

        logger.LogInformation("Player {TargetId} kicked from lobby {LobbyId}", targetPlayerId, lobbyId);
    }

    public async Task ChangeColor(string color)
    {
        var (userId, lobbyId, _) = GetCallerContext();
        var lobby = lobbyStore.Get(lobbyId)
            ?? throw new HubException("Lobby not found.");
#if DEBUG
        Log.Information($"chaning color to {color}");
#endif
        lobby.ChangePlayerColor(userId, color);
        await broadcaster.BroadcastLobbyState(lobby);
    }

    public async Task UpdateRaceState(int index, int mistakes)
    {
        var (userId, lobbyId, _) = GetCallerContext();
        await updateProgressHandler.Handle(new UpdateProgressCommand(userId, lobbyId, index, mistakes));
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

        var result = await disconnectHandler.Handle(
            new DisconnectCommand(Context.ConnectionId, lobbyId, playerId));

        await Clients.Group(groupName).SendAsync("PlayerDisconnected", new { playerId = result.PlayerId });

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        var lobby = lobbyStore.Get(lobbyId);
        if (lobby != null)
            await broadcaster.BroadcastLobbyState(lobby);

        logger.LogInformation("Player {PlayerId} disconnected from lobby {LobbyId}", playerId, lobbyId);
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
            await fastReconnectHandler.Handle(new FastReconnectCommand(lobbyId, playerId));
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

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

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
