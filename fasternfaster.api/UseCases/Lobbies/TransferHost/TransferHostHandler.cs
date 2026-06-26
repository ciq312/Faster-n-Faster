using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public class TransferHostHandler(ILobbyService lobbyService) : IRequestHandler<TransferHostCommand>
{
    public async Task Handle(TransferHostCommand command, CancellationToken cancellationToken)
    {
        await lobbyService.TransferHost(command.HostId, command.UserId);
    }
}
