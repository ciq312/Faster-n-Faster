using FasterNFaster.Api.UseCases.Lobbies.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.KickPlayer;

public class KickPlayerHandler(ILobbyStore lobbies) : IRequestHandler<KickPlayerCommand, KickPlayerResult>
{
    public async Task<KickPlayerResult> Handle(KickPlayerCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.TargetPlayerId);

        await lobbies.Mutate(lobbyId, l => l.Kick(command.UserId, command.TargetPlayerId));

        return new KickPlayerResult(command.TargetPlayerId);
    }
}
