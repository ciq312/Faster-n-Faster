using FastEndpoints;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Exceptions.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using FasterNFaster.Api.UseCases.Lobbies.FastReconnect;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Results;
using FasterNFaster.Api.UseCases.Lobbies.KickPlayer;
using FasterNFaster.Api.UseCases.Lobbies.Refresh;
using FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;
using FasterNFaster.Api.UseCases.Lobbies.StartRace;
using FasterNFaster.Api.UseCases.Lobbies.TransferHost;
using FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Hubs;

[Authorize]
public class GameHub(
    ILogger<GameHub> logger,
    ILobbyStore lobbyStore,
    ILobbyService lobbyService,
    ILobbySessionService lobbySessionService,
    IBanService banService,
    ISessionService sessionService,
    LobbyStateBroadcaster broadcaster,
    IHandler<JoinLobbyCommand> joinHandler,
    IHandler<StartRaceCommand> startRaceHandler,
    IHandler<TransferHostCommand> transferHostHandler,
    IHandler<KickPlayerCommand, KickPlayerResult> kickPlayerHandler,
    IHandler<UpdateProgressCommand> updateProgressHandler,
    IHandler<DisconnectCommand> disconnectHandler,
    IHandler<FastReconnectCommand> fastReconnectHandler,
    IHandler<RefreshPassageCommand> refreshPassageHandler,
    IHandler<RefreshCommand> refreshHandler) : Hub
{

    private (Guid UserId, string Nick, string Role) GetCallerContext()
    {
        var userIdClaim = Context.User?.FindFirst("sub")?.Value
            ?? throw new HubException("Not authenticated.");

        var nick = Context.User?.FindFirst("name")?.Value
            ?? throw new HubException("Not authenticated.");

        var userId = Guid.Parse(userIdClaim);

        var role = Context.User?.FindFirst("role")?.Value
            ?? throw new HubException("Not authenticated.");

        return (userId, nick, role);
    }

    private Task<LobbyContext> RequireLobbyContext()
    {
        var lobbyId = lobbyService.GetLobbyIdOfPlayer(GetCallerContext().UserId)
            ?? throw new InvalidOperationException("Not in lobby");

        var groupName = $"lobby-{lobbyId}";
        return Task.FromResult(new LobbyContext(lobbyId, groupName));
    }

    private record LobbyContext(Guid LobbyId, string GroupName);

    public override async Task OnConnectedAsync()
    {
        logger.LogDebug("Connection established: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();

        var userId = GetCallerContext().UserId;
        if (await banService.IsBannedAsync(userId))
        {
            await Clients.Caller.SendAsync("Banned", "You are banned");
            Context.Abort();
            return;
        }

        await StoreSession(userId, Context.ConnectionId);
    }

    private async Task StoreSession(Guid userId, string callerConnectionId)
    {
        var previousSessionId = sessionService.GetActiveSession(userId);
        if (previousSessionId != null && previousSessionId != callerConnectionId)
        {
#if DEBUG
            Log.Information($"Handling another session");
#endif
            await HandleSessionRestart(userId, callerConnectionId, previousSessionId);
        }

        logger.LogDebug($"previous sessionId : {previousSessionId} and callerId : {callerConnectionId}");

        sessionService.SetUserSession(userId, callerConnectionId);
        await AddToGroupIfInLobby(userId, callerConnectionId);

    }

    private async Task HandleSessionRestart(Guid userId, string callerConnectionId, string previousSession)
    {
        sessionService.ClearActiveSession(userId);
        await Clients.Client(previousSession).SendAsync("AnotherSessionStarted");
    }

    private async Task AddToGroupIfInLobby(Guid userId, string callerConnectionId)
    {
        var lobbyId = lobbyService.GetLobbyIdOfPlayer(userId);
        if (lobbyId is null) return;

        logger.LogDebug($"adding  user {userId} to group lobby-{lobbyId} ");

        await Groups.AddToGroupAsync(callerConnectionId, $"lobby-{lobbyId}");
    }

    public async Task ConnectToLobby(Guid lobbyId, string? inviteCode = null)
    {
        var userId = GetCallerContext().UserId;

        if (await banService.IsBannedAsync(userId))
        {
            await Clients.Caller.SendAsync("Banned", "You are banned");
            Context.Abort();
            return;
        }

        var nick = GetCallerContext().Nick;
        var role = GetCallerContext().Role;
        await joinHandler.Handle(new JoinLobbyCommand(userId, lobbyId, nick, role, inviteCode!));

        var groupName = $"lobby-{lobbyId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var lobby = lobbyStore.GetRequired(lobbyId);

        await broadcaster.BroadcastLobbyState(lobby);

        await Clients.OthersInGroup(groupName)
            .SendAsync("PlayerJoined", new { playerId = userId, displayName = nick });

        logger.LogDebug("Player {PlayerId} connected to lobby {LobbyId}", userId, lobbyId);
    }

    public async Task RefreshLobby()
    {
        LobbyContext lobbyContext = await RequireLobbyContext();

        Lobby lobby = lobbyStore.GetRequired(lobbyContext.LobbyId);

        await refreshHandler.Handle(new RefreshCommand(lobby.Id, GetCallerContext().UserId));

        await broadcaster.BroadcastLobbyState(lobby);
    }
    public async Task StartRace()
    {
        var userId = GetCallerContext().UserId;

        var lobbyContext = await RequireLobbyContext();

        await startRaceHandler.Handle(new StartRaceCommand(userId, lobbyContext.LobbyId));

        await Clients.Group(lobbyContext.GroupName).SendAsync("RaceStarting", new { countdownSeconds = 3 });

        logger.LogDebug("Race starting in lobby {LobbyId} by host {PlayerId}", lobbyContext.LobbyId, userId);
    }

    public async Task RefreshPassage()
    {
        var userId = GetCallerContext().UserId;
        var lobbyContext = await RequireLobbyContext();

        await refreshPassageHandler.Handle(new RefreshPassageCommand(userId, lobbyContext.LobbyId));
    }

    public async Task TransferHost(Guid targetPlayerId)
    {
        var userId = GetCallerContext().UserId;
        var lobbyContext = await RequireLobbyContext();

        await transferHostHandler.Handle(new TransferHostCommand(userId, lobbyContext.LobbyId, targetPlayerId));

        logger.LogDebug("Host transferred from {OldHost} to {NewHost} in lobby {LobbyId}",
            userId, targetPlayerId, lobbyContext.LobbyId);
    }

    public async Task KickPlayer(Guid targetPlayerId)
    {
        var userId = GetCallerContext().UserId;
        var lobbyContext = await RequireLobbyContext();

        await kickPlayerHandler.Handle(new KickPlayerCommand(userId, lobbyContext.LobbyId, targetPlayerId));

        logger.LogDebug("Player {TargetId} kicked from lobby {LobbyId}", targetPlayerId, lobbyContext.LobbyId);
    }

    public async Task ChangeColor(string color)
    {
        var lobbyContext = await RequireLobbyContext();
        var userId = GetCallerContext().UserId;

        logger.LogDebug($"chaning color to {color}");

        await lobbyService.ChangePlayerColor(lobbyContext.LobbyId, userId, color);
        await broadcaster.BroadcastLobbyState(lobbyContext.LobbyId);
    }

    public async Task UpdateRaceState(int index, int mistakes, string typed)
    {
        var userId = GetCallerContext().UserId;
        var lobbyContext = await RequireLobbyContext();

        try
        {
            await updateProgressHandler.Handle(new UpdateProgressCommand(userId, lobbyContext.LobbyId, index, mistakes, typed));
        }
        catch (CheaterDetectedException ex)
        {
            await banService.BanAsync(userId, ex.Reason);
            await lobbySessionService.RemovePlayerFromLobby(userId);
            await Clients.Caller.SendAsync("Banned", $"Cheating detected: {ex.Reason}");
            Context.Abort();
        }
    }

    public async Task LeaveLobby()
    {
        var (playerId, nick, role) = GetCallerContext();
        var lobbyContext = await RequireLobbyContext();

        await disconnectHandler.Handle(new DisconnectCommand(playerId));

        logger.LogDebug("Player {PlayerId} disconnected from lobby {LobbyId}", playerId, lobbyContext.LobbyId);
    }
    public Task<long> Ping(long clientSentMs) => Task.FromResult(clientSentMs);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCallerContext().UserId;
        var maybeLobbyId = lobbyService.GetLobbyIdOfPlayer(userId);

        if (maybeLobbyId is null)
        {
            await base.OnDisconnectedAsync(exception);
            return;
        }

        var lobbyId = maybeLobbyId.Value;
        string groupName = $"lobby-{lobbyId}";


        try
        {
            await fastReconnectHandler.Handle(new FastReconnectCommand(lobbyId, userId));
            await disconnectHandler.Handle(new DisconnectCommand(userId));

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            var lobby = lobbyStore.Get(lobbyId);
            if (lobby != null)
                await broadcaster.BroadcastLobbyState(lobby);

            sessionService.ClearActiveSession(userId);

            logger.LogDebug("Player {PlayerId} disconnected from lobby {LobbyId}", userId, lobbyId);

            await base.OnDisconnectedAsync(exception);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Fast reconnect for player happened");
            return;
        }
    }

}
