using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Disconnect;

public class DisconnectHandler(ILobbySessionService lobbySessionService) : IRequestHandler<DisconnectCommand>
{
    public async Task Handle(DisconnectCommand command, CancellationToken cancellationToken)
    {
        await lobbySessionService.RemovePlayerFromLobby(command.PlayerId);
    }
}
