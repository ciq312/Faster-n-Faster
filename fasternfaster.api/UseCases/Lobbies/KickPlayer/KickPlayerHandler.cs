using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.UseCases.Lobbies.KickPlayer;

public class KickPlayerHandler(ILobbySessionService lobbySessionService) : IHandler<KickPlayerCommand, KickPlayerResult>
{
    private readonly ILobbySessionService lobbySessionService = lobbySessionService;

    public async Task<KickPlayerResult> Handle(KickPlayerCommand command)
    {

        await lobbySessionService.KickPlayer(command.UserId, command.TargetPlayerId);

#if DEBUG
        Log.Information($"kicked player with id {command.TargetPlayerId} in lobby {command.LobbyId}");
#endif

        return new KickPlayerResult(command.TargetPlayerId);
    }
}
