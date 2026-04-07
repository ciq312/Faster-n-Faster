using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Infrastructure;
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

        // Reconnects bypass invite code check
        if (_lobbyService.TryGetPendingRemoval(command.LobbyId, command.PlayerId, out var cts))
        {
            Log.Information("Cancelling pending removal for player {PlayerId} in lobby {LobbyId}",
                command.PlayerId, command.LobbyId);
            cts.Cancel();
            return new JoinLobbyResult(IsReconnect: true);
        }

        // Existing players (e.g. page refresh) bypass invite code check
        if (!lobby.IsPlayerInLobby(command.PlayerId))
        {
            if (lobby.LobbySettings.IsPrivate &&
                !string.Equals(lobby.LobbySettings.InviteCode, command.InviteCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid invite code.");
            }

            var user = await _userRepo.GetByIdAsync(command.PlayerId);
            lobby.AddPlayer(user!);
        }

        Log.Information("Player {PlayerId} joined lobby {LobbyId}", command.PlayerId, lobby.Id);

        return new JoinLobbyResult(IsReconnect: false);
    }
}
