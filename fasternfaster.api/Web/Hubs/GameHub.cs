using System.Security.Claims;
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
    private readonly IHandler<JoinLobbyCommand, JoinLobbyResult> _joinHandler;
    private readonly ILobbyService _lobbyService;

    public GameHub(
        ILogger<GameHub> logger,
        ILobbyStore lobbyStore,
        IHandler<JoinLobbyCommand, JoinLobbyResult> joinHandler,
        ILobbyService lobbyService
    )
    {
        _logger = logger;
        _lobbyStore = lobbyStore;
        _joinHandler = joinHandler;
        _lobbyService = lobbyService;
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
            var command = new JoinLobbyCommand(userId, lobbyId);
            var result = await _joinHandler.Handle(command);

            _lobbyService.TrackConnection(Context.ConnectionId, lobbyId, userId);

            // Set the SignalR connection ID on the player
            var lobby = _lobbyStore.Get(lobbyId)!;
            var player = lobby.Players.First(p => p.PlayerId == userId);
            player.Reconnect(Context.ConnectionId);

            var groupName = $"lobby-{lobbyId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // Full lobby state to the joining player
            await Clients.Caller.SendAsync("LobbyState", result);

            // Notify others
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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var conn = _lobbyService.GetConnection(Context.ConnectionId);
        if (conn != null)
        {
            var (lobbyId, playerId) = conn.Value;
            _lobbyService.RemoveConnection(Context.ConnectionId);

            var lobby = _lobbyStore.Get(lobbyId);
            if (lobby != null)
            {
                var player = lobby.Players.FirstOrDefault(p => p.PlayerId == playerId);
                if (player != null)
                {
                    player.Disconnect();

                    var groupName = $"lobby-{lobbyId}";
                    await Clients
                        .Group(groupName)
                        .SendAsync("PlayerDisconnected", new { playerId });

                    // Host promotion: if host left, promote next connected player by join order
                    if (lobby.HostPlayerId == playerId)
                    {
                        var nextHost = lobby
                            .Players.Where(p => p.IsConnected && p.PlayerId != playerId)
                            .OrderBy(p => p.JoinOrder)
                            .FirstOrDefault();

                        lobby.AssignHost(nextHost?.PlayerId);

                        if (nextHost != null)
                        {
                            await Clients
                                .Group(groupName)
                                .SendAsync(
                                    "HostChanged",
                                    new { newHostPlayerId = nextHost.PlayerId }
                                );
                        }
                    }

                    _logger.LogInformation(
                        "Player {PlayerId} disconnected from lobby {LobbyId}",
                        playerId,
                        lobbyId
                    );
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
