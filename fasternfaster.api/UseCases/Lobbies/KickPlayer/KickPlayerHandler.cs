using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.KickPlayer;

public class KickPlayerHandler(ILobbySessionService lobbySessionService) : IRequestHandler<KickPlayerCommand, KickPlayerResult>
{
    public async Task<KickPlayerResult> Handle(KickPlayerCommand command, CancellationToken cancellationToken)
    {
        await lobbySessionService.KickPlayer(command.UserId, command.TargetPlayerId);
        return new KickPlayerResult(command.TargetPlayerId);
    }
}
