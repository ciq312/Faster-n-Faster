using FastEndpoints;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Results;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Handlers;

public class JoinLobbyHandler(ILobbyStore lobbyStore, IUserRepository userRepo, ILobbyService lobbyService) : IHandler<JoinLobbyCommand, JoinLobbyResult>
{
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly IUserRepository userRepo = userRepo;
    private readonly ILobbyService lobbyService = lobbyService;

    public async Task<JoinLobbyResult> Handle(JoinLobbyCommand command)
    {
        var lobby = lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        if (lobby.IsSessionActive) throw new InvalidOperationException("Can't join active lobby");

        if (lobbyService.TryGetPendingRemoval(command.LobbyId, command.PlayerId, out var cts))
        {
#if DEBUG
            Log.Information("Cancelling pending removal for player {PlayerId} in lobby {LobbyId}",
                command.PlayerId, command.LobbyId);
#endif
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

            var user = await userRepo.GetByIdAsync(command.PlayerId) ?? throw new UserNotFoundException(command.PlayerId);
            lobby.AddPlayer(user!);
        }
#if DEBUG
        Log.Information("Player {PlayerId} joined lobby {LobbyId}", command.PlayerId, lobby.Id);
#endif
        return new JoinLobbyResult(IsReconnect: false);
    }
}
