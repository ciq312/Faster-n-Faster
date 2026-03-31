using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.KickPlayer;

public class KickPlayerHandler : IHandler<KickPlayerCommand, KickPlayerResult>
{
    private readonly ILobbyStore _lobbyStore;
    private readonly ILobbyService _lobbyService;

    public KickPlayerHandler(ILobbyStore lobbyStore, ILobbyService lobbyService, IRaceTickRegistry registry)
    {
        _lobbyStore = lobbyStore;
        _lobbyService = lobbyService;
    }

    public Task<KickPlayerResult> Handle(KickPlayerCommand command)
    {
        var lobby = _lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        lobby.KickPlayer(command.UserId, command.TargetPlayerId);

        var kickedConnectionId = _lobbyService.GetConnectionId(command.LobbyId, command.TargetPlayerId);
        if (kickedConnectionId != null)
            _lobbyService.RemoveConnection(kickedConnectionId);

        return Task.FromResult(new KickPlayerResult(command.TargetPlayerId, kickedConnectionId));
    }
}
