using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Handlers;

public class JoinLobbyHandler : IHandler<JoinLobbyCommand>
{
    private readonly ILobbyStore _lobbyStore;
    private readonly IUserRepository _userRepo;

    public JoinLobbyHandler(ILobbyStore lobbyStore, IUserRepository userRepo)
    {
        _lobbyStore = lobbyStore;
        _userRepo = userRepo;
    }

    public async Task Handle(JoinLobbyCommand command)
    {
        var lobby = _lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        var user = await _userRepo.GetByIdAsync(command.PlayerId)
            ?? throw new UserNotFoundException(command.PlayerId);

        if (!lobby.IsPlayerInLobby(user.Id))
            lobby.AddPlayer(user);

        Log.Information("Player {PlayerId} joined lobby {LobbyId}", command.PlayerId, lobby.Id);
    }
}
