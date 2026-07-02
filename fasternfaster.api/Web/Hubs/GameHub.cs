using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.ChangeColor;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using FasterNFaster.Api.UseCases.Lobbies.FastReconnect;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.KickPlayer;
using FasterNFaster.Api.UseCases.Lobbies.Refresh;
using FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;
using FasterNFaster.Api.UseCases.Lobbies.StartRace;
using FasterNFaster.Api.UseCases.Lobbies.TransferHost;
using FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.Hubs;

[Authorize]
public partial class GameHub(
    ILogger<GameHub> logger,
    ILobbyStore lobbyStore,
    ILobbyService lobbyService,
    ISessionService sessionService,
    ILobbyStateBroadcaster broadcaster,
    ISender sender) : Hub
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

    public override async Task OnConnectedAsync()
    {
        logger.LogDebug("Connection established: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();

        var userId = GetCallerContext().UserId;
        await StoreSession(userId, Context.ConnectionId);
    }

    private async Task StoreSession(Guid userId, string callerConnectionId)
    {
        var previousSessionId = sessionService.GetActiveSession(userId);
        if (previousSessionId != null && previousSessionId != callerConnectionId)
        {
            logger.LogDebug("Handling another session for user {UserId}", userId);
            await HandleSessionRestart(userId, callerConnectionId, previousSessionId);
        }

        logger.LogDebug("Previous sessionId: {PreviousSession}, callerId: {CallerId}", previousSessionId, callerConnectionId);

        sessionService.SetUserSession(userId, callerConnectionId);
    }

    private async Task HandleSessionRestart(Guid userId, string callerConnectionId, string previousSession)
    {
        sessionService.ClearActiveSession(userId);
        await Clients.Client(previousSession).SendAsync(Methods.AnotherSessionStarted);
    }

    public async Task ConnectToLobby(Guid lobbyId, string? inviteCode = null)
    {
        var (userId, nick, role) = GetCallerContext();

        await sender.Send(new JoinLobbyCommand(userId, lobbyId, nick, role, inviteCode!));

        logger.LogDebug("Player {PlayerId} connected to lobby {LobbyId}", userId, lobbyId);
    }

    public async Task RefreshLobby()
    {
        await sender.Send(new RefreshCommand(GetCallerContext().UserId));
    }

    public async Task StartRace()
    {
        var userId = GetCallerContext().UserId;
        var lobbyId = await sender.Send(new StartRaceCommand(userId));

        logger.LogDebug("Race starting in lobby {LobbyId} by host {PlayerId}", lobbyId, userId);
    }

    public async Task RefreshPassage()
    {
        await sender.Send(new RefreshPassageCommand(GetCallerContext().UserId));
    }

    public async Task TransferHost(Guid targetPlayerId)
    {
        var userId = GetCallerContext().UserId;

        await sender.Send(new TransferHostCommand(userId, targetPlayerId));

        logger.LogDebug("Host transferred from {OldHost} to {NewHost}", userId, targetPlayerId);
    }

    public async Task KickPlayer(Guid targetPlayerId)
    {
        var userId = GetCallerContext().UserId;

        await sender.Send(new KickPlayerCommand(userId, targetPlayerId));

        logger.LogDebug("Player {TargetId} kicked by {UserId}", targetPlayerId, userId);
    }

    public async Task ChangeColor(string color)
    {
        var userId = GetCallerContext().UserId;

        logger.LogDebug("Changing color to {Color} for user {UserId}", color, userId);

        await sender.Send(new ChangeColorCommand(userId, color));
    }

    public async Task UpdateRaceState(int index, int mistakes, string typed)
    {
        var userId = GetCallerContext().UserId;
        await sender.Send(new UpdateProgressCommand(userId, index, mistakes, typed));
    }

    public async Task LeaveLobby()
    {
        var (playerId, _, _) = GetCallerContext();

        await sender.Send(new DisconnectCommand(playerId));

        logger.LogDebug("Player {PlayerId} left lobby", playerId);
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

        try
        {
            await sender.Send(new FastReconnectCommand(lobbyId, userId));
            await sender.Send(new DisconnectCommand(userId));

            var lobby = lobbyStore.Get(lobbyId);
            if (lobby != null)
                await broadcaster.BroadcastLobbyState(lobby);

            sessionService.ClearActiveSession(userId);

            logger.LogDebug("Player {PlayerId} disconnected from lobby {LobbyId}", userId, lobbyId);

            await base.OnDisconnectedAsync(exception);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Fast reconnect for player {PlayerId}", userId);
        }
    }
}
