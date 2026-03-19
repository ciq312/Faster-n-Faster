using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Helpers;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Results;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Handlers;

public class JoinLobbyHandler : IHandler<JoinLobbyCommand, JoinLobbyResult>
{
    private readonly ILobbyStore _lobbyStore;
    private readonly IUserRepository _userRepo;

    public JoinLobbyHandler(ILobbyStore lobbyStore, IUserRepository userRepo)
    {
        _lobbyStore = lobbyStore;
        _userRepo = userRepo;
    }

    public Task<JoinLobbyResult> Handle(JoinLobbyCommand command)
    {
        var lobby = _lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        // No multi-lobby: check player isn't already in another lobby
        var existingLobby = _lobbyStore.GetAll()
            .FirstOrDefault(l => l.Players.Any(p => p.PlayerId == command.PlayerId && p.IsConnected));
        if (existingLobby != null && existingLobby.Id != command.LobbyId)
            throw new InvalidOperationException("Player is already in another lobby.");

        var user = _userRepo.Get(command.PlayerId)
            ?? throw new KeyNotFoundException("User not found.");

        // Entity enforces: status, capacity, private, duplicate
        var player = lobby.AddPlayer(command.PlayerId);

        var gameMode = lobby.WordRace != null ? "wordcount"
            : lobby.TimerRace != null ? "timer"
            : (string?)null;

        var players = lobby.Players
            .OrderBy(p => p.JoinOrder)
            .Select(p =>
            {
                var u = _userRepo.Get(p.PlayerId);
                return new LobbyPlayerDto(p.PlayerId, u?.Nick ?? "Unknown", p.JoinOrder, p.IsConnected);
            })
            .ToList();

        Log.Information("Player {PlayerId} joined lobby {LobbyId}", command.PlayerId, lobby.Id);

        return Task.FromResult(new JoinLobbyResult(
            lobby.Id, lobby.Name, gameMode, lobby.IsPrivate,
            command.PlayerId, lobby.HostPlayerId!.Value, players));
    }
}
