using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Lobbies.Services;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public class TransferHostHandler(ILobbyStore lobbies, ILobbyStateTracker tracker) : IRequestHandler<TransferHostCommand>
{
    public async Task Handle(TransferHostCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.TargetPlayerId);

        await lobbies.Mutate(lobbyId, l => l.TransferHost(command.HostId, command.TargetPlayerId));
        tracker.MarkChanged(lobbyId);
    }
}
