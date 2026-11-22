using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.KickPlayer;

public class KickPlayerHandler(
    ILobbyStore lobbies,
    ILogger<KickPlayerHandler> logger) : IRequestHandler<KickPlayerCommand, KickPlayerResult>
{
    public async Task<KickPlayerResult> Handle(KickPlayerCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.TargetPlayerId);

        await lobbies.Mutate(lobbyId, l => l.Kick(command.UserId, command.TargetPlayerId));

        logger.LogInformation("Player {TargetPlayerId} was kicked by {UserId} in lobby {LobbyId} {TelemetryOperation}",
            command.TargetPlayerId,
            command.UserId,
            lobbyId,
            TelemetryOperations.PlayerKicked);

        return new KickPlayerResult(command.TargetPlayerId);
    }
}
