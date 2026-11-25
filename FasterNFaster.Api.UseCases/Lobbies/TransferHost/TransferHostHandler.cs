using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public class TransferHostHandler(
    ILobbyStore lobbies,
    ILogger<TransferHostHandler> logger
    ) : IRequestHandler<TransferHostCommand>
{
    public async Task Handle(TransferHostCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.TargetPlayerId);

        await lobbies.Mutate(lobbyId, l => l.TransferHost(command.HostId, command.TargetPlayerId));

        logger.LogInformation("Host was transferred to {TargetPlayerId} from {HostId} in lobby {LobbyId} {TelemetryOperation}",
            command.TargetPlayerId,
            command.HostId,
            lobbyId,
            TelemetryOperations.HostTransferred);
    }
}
