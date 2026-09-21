using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.ChangeColor;

public class ChangeColorHandler(ILobbyAccess lobbies) : IRequestHandler<ChangeColorCommand>
{
    public async Task Handle(ChangeColorCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.UserId);
        await lobbies.Mutate(lobbyId, l => l.ChangePlayerColor(command.UserId, command.Color));
    }
}
