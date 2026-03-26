using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.Disconnect;

public class DisconnectHandler : IHandler<DisconnectCommand, DisconnectResult>
{
    private readonly ILobbyStore _lobbyStore;
    private readonly ILobbyService _lobbyService;
    private readonly IRaceTickRegistry _raceTickRegistry;

    public DisconnectHandler(ILobbyStore lobbyStore, ILobbyService lobbyService, IRaceTickRegistry raceTickRegistry)
    {
        _lobbyStore = lobbyStore;
        _lobbyService = lobbyService;
        _raceTickRegistry = raceTickRegistry;
    }

    public Task<DisconnectResult> Handle(DisconnectCommand command)
    {
        _lobbyService.RemoveConnection(command.ConnectionId);

        var lobby = _lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        var player = lobby.Players.FirstOrDefault(p => p.User.Id == command.PlayerId)
            ?? throw new KeyNotFoundException($"Player not found in lobby {command.LobbyId}.");

        player.Disconnect();

        // Host promotion
        Guid? newHostId = null;
        if (lobby.HostId == command.PlayerId)
        {
            var nextHost = lobby.Players
                .Where(p => p.IsConnected && p.User.Id != command.PlayerId)
                .OrderBy(p => p.JoinOrder)
                .FirstOrDefault();

            if (nextHost != null)
            {
                lobby.AssignHost(nextHost.User.Id);
                newHostId = nextHost.User.Id;
            }
        }

        // Deregister ticks if no connected players remain during race
        bool shouldDeregister = lobby.CurrentStatus == Lobby.Status.racing
            && !lobby.Players.Any(p => p.IsConnected);

        if (shouldDeregister)
            _raceTickRegistry.DeregisterLobby(command.LobbyId);

        return Task.FromResult(new DisconnectResult(command.PlayerId, newHostId, shouldDeregister));
    }
}
