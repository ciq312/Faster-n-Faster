using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.Web.Lobbies.LobbyState;

namespace FasterNFaster.Api.UseCases.Lobbies.KickPlayer;

public class KickPlayerHandler(ILobbyStore lobbyStore, ILobbyService lobbyService, LobbyStateBroadcaster broadcaster) : IHandler<KickPlayerCommand, KickPlayerResult>
{
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly ILobbyService lobbyService = lobbyService;
    private readonly LobbyStateBroadcaster broadcaster = broadcaster;

    public async Task<KickPlayerResult> Handle(KickPlayerCommand command)
    {
        var lobby = lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        lobby.KickPlayer(command.UserId, command.TargetPlayerId);

        var kickedConnectionId = lobbyService.GetConnectionId(command.LobbyId, command.TargetPlayerId);
        if (kickedConnectionId != null)
            lobbyService.RemoveConnection(kickedConnectionId);

#if DEBUG
        Log.Information($"kicked player with id {command.TargetPlayerId} in lobby {command.LobbyId}");
#endif

        await broadcaster.BroadcastLobbyState(lobby);

        return new KickPlayerResult(command.TargetPlayerId, kickedConnectionId);
    }
}
