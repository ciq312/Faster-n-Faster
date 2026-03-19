using FasterNFaster.Api.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Infrastructure.Hubs;

public class GameHub : Hub
{
    private readonly ILogger<GameHub> _logger;
    private readonly ILobbyStore _lobbyStore;

    public GameHub(ILogger<GameHub> logger, ILobbyStore lobbyStore)
    {
        _logger = logger;
        _lobbyStore = lobbyStore;
    }

    // public async Task ConnectToLobby(Guid lobbyId, Guid playerId)
    // {
    //     var lobby = _lobbyRepo.Get(lobbyId);
    //     if (lobby == null)
    //     {
    //         await Clients.Caller.SendAsync("Error", "Lobby not found.");
    //         return;
    //     }

    //     var player = lobby.Players.FirstOrDefault(p => p.PlayerId == playerId);
    //     if (player == null)
    //     {
    //         await Clients.Caller.SendAsync("Error", "Player not found in lobby.");
    //         return;
    //     }

    //     player.Reconnect(Context.ConnectionId);

    //     var groupName = $"lobby-{lobbyId}";
    //     await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

    //     await Clients.OthersInGroup(groupName).SendAsync("PlayerConnected", new
    //     {
    //         playerId = player.PlayerId,
    //         displayName = player.DisplayName,
    //         joinOrder = player.JoinOrder
    //     });

    //     var players = lobby.Players
    //         .OrderBy(p => p.JoinOrder)
    //         .Select(p => new
    //         {
    //             id = p.PlayerId,
    //             displayName = p.DisplayName,
    //             joinOrder = p.JoinOrder,
    //             isConnected = p.IsConnected
    //         })
    //         .ToList();

    //     await Clients.Caller.SendAsync("LobbyState", new
    //     {
    //         lobbyId = lobby.Id,
    //         lobbyName = lobby.Name,
    //         gameMode = lobby.WordRace != null ? "wordcount"
    //             : lobby.TimerRace != null ? "timer"
    //             : (string?)null,
    //         isPrivate = lobby.IsPrivate,
    //         hostPlayerId = lobby.HostPlayerId,
    //         players
    //     });

    //     _logger.LogInformation("Player {PlayerId} connected to lobby {LobbyId}", playerId, lobbyId);
    // }

    // public override async Task OnDisconnectedAsync(Exception? exception)
    // {
    //     await base.OnDisconnectedAsync(exception);
    // }

    // public async Task Ping()
    // {
    //     await Clients.Caller.SendAsync("Pong", DateTime.UtcNow.ToString("O"));
    // }
}
