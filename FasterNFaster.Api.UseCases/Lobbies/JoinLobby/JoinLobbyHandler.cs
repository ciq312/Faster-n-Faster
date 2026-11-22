using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby;

public class JoinLobbyHandler(
    ILobbyStore lobbies,
    ILogger<JoinLobbyHandler> logger
    ) : IRequestHandler<JoinLobbyCommand>
{
    public async Task Handle(JoinLobbyCommand command, CancellationToken cancellationToken)
    {
        await lobbies.Mutate(command.LobbyId, l => l.Join(
            command.PlayerId,
            command.Nick,
            command.InviteCode,
            id => lobbies.TryClaimPlayer(id, command.LobbyId)));

        logger.LogInformation("Player {PlayerId} joined lobby {LobbyId} {TelemetryOperation}",
            command.PlayerId,
            command.LobbyId,
            TelemetryOperations.PlayerJoined);
    }

}
