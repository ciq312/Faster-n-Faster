using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Results;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Handlers;

public class JoinLobbyHandler : IHandler<JoinLobbyCommand, JoinLobbyResult>
{
    private readonly ILobbyStore _lobbyStore;
    private readonly IUserRepository _userRepo;
    private readonly ILobbyService _lobbyService;

    public JoinLobbyHandler(ILobbyStore lobbyStore, IUserRepository userRepo, ILobbyService lobbyService)
    {
        _lobbyStore = lobbyStore;
        _userRepo = userRepo;
        _lobbyService = lobbyService;
    }

    public async Task<JoinLobbyResult> Handle(JoinLobbyCommand command)
    {
        var lobby = _lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        if (_lobbyService.TryGetPendingRemoval(command.LobbyId, command.PlayerId, out var cts))
        {
            Log.Information("Cancelling pending removal for player {PlayerId} in lobby {LobbyId}",
                command.PlayerId, command.LobbyId);
            cts.Cancel();
            return new JoinLobbyResult(IsReconnect: true);
        }

        var user = await _userRepo.GetByIdAsync(command.PlayerId)
            ?? throw new UserNotFoundException(command.PlayerId);

        if (!lobby.IsPlayerInLobby(user.Id))
            lobby.AddPlayer(user);

        Log.Information("Player {PlayerId} joined lobby {LobbyId}", command.PlayerId, lobby.Id);

        return new JoinLobbyResult(IsReconnect: false);
    }
}
