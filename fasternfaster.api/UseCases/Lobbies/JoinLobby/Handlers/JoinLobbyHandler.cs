using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Helpers;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Results;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Handlers;

public class JoinLobbyHandler : IHandler<JoinLobbyCommand, JoinLobbyResult>
{
    private readonly ILobbyStore _lobbyStore;

    public JoinLobbyHandler(ILobbyStore lobbyStore)
    {
        _lobbyStore = lobbyStore;
    }

    public Task<JoinLobbyResult> Handle(JoinLobbyCommand command)
    {
        var lobby =
            _lobbyStore.Get(command.LobbyId) ?? throw new KeyNotFoundException("Lobby not found.");

        //     if (lobby.Status != "waiting")
        //         throw new InvalidOperationException("Lobby is not accepting players.");

        //     if (lobby.Players.Count >= lobby.MaxPlayers)
        //         throw new InvalidOperationException("Lobby is full.");

        //     var nextJoinOrder = lobby.Players.Any()
        //         ? lobby.Players.Max(p => p.JoinOrder) + 1
        //         : 1;

        //     // var player = new Core.Entities.LobbyPlayer(lobby.Id, command.DisplayName, nextJoinOrder);
        //     lobby.Players.Add(player);

        //     var gameMode = lobby.WordRace != null ? "wordcount"
        //         : lobby.TimerRace != null ? "timer"
        //         : null;

        //     var players = lobby.Players
        //         .OrderBy(p => p.JoinOrder)
        //         .Select(p => new LobbyPlayerDto(p.Id, p.DisplayName, p.JoinOrder, p.IsConnected))
        //         .ToList();

        throw new NotImplementedException();
    }
}
