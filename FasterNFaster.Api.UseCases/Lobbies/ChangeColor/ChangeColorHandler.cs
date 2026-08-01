using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.ChangeColor;

public class ChangeColorHandler(ILobbyService lobbyService, ILobbyStateBroadcaster broadcaster) : IRequestHandler<ChangeColorCommand>
{
    public async Task Handle(ChangeColorCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbyService.GetLobbyIdOfPlayerRequired(command.UserId);
        await lobbyService.ChangePlayerColor(lobbyId, command.UserId, command.Color);
        await broadcaster.BroadcastLobbyState(lobbyId);
    }
}
