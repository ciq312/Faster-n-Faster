using FasterNFaster.Api.UseCases.Lobbies.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public class TransferHostHandler(ILobbyStore lobbies) : IRequestHandler<TransferHostCommand>
{
    public async Task Handle(TransferHostCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.TargetPlayerId);

        await lobbies.Mutate(lobbyId, l => l.TransferHost(command.HostId, command.TargetPlayerId));
    }
}
