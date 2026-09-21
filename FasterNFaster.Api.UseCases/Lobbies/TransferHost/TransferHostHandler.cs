using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public class TransferHostHandler(ILobbyAccess lobbies) : IRequestHandler<TransferHostCommand>
{
    public async Task Handle(TransferHostCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.TargetPlayerId);

        await lobbies.Mutate(lobbyId, l => l.TransferHost(command.HostId, command.TargetPlayerId));
    }
}
