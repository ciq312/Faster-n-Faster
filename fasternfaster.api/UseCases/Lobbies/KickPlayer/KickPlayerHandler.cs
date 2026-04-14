using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.KickPlayer;

public class KickPlayerHandler(ILobbyStore lobbyStore, ILobbyService lobbyService) : IHandler<KickPlayerCommand, KickPlayerResult>
{
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly ILobbyService lobbyService = lobbyService;

    public Task<KickPlayerResult> Handle(KickPlayerCommand command)
    {
        var lobby = lobbyStore.GetRequired(command.LobbyId);

        lobby.KickPlayer(command.UserId, command.TargetPlayerId);

        var kickedConnectionId = lobbyService.GetConnectionId(command.LobbyId, command.TargetPlayerId);
        if (kickedConnectionId != null)
            lobbyService.RemoveConnection(kickedConnectionId);

        if (lobby.Race.Participants.TryGetValue(command.TargetPlayerId, out var participant))
            participant.MarkWithdrawn();

#if DEBUG
        Log.Information($"kicked player with id {command.TargetPlayerId} in lobby {command.LobbyId}");
#endif

        return Task.FromResult(new KickPlayerResult(command.TargetPlayerId, kickedConnectionId));
    }
}
