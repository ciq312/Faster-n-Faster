using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.UseCases.Lobbies.KickPlayer;

public class KickPlayerHandler(ILobbyService lobbyService) : IHandler<KickPlayerCommand, KickPlayerResult>
{
    private readonly ILobbyService lobbyService = lobbyService;

    public Task<KickPlayerResult> Handle(KickPlayerCommand command)
    {

        lobbyService.KickPlayer(command.UserId, command.TargetPlayerId);

#if DEBUG
        Log.Information($"kicked player with id {command.TargetPlayerId} in lobby {command.LobbyId}");
#endif

        return Task.FromResult(new KickPlayerResult(command.TargetPlayerId));
    }
}
