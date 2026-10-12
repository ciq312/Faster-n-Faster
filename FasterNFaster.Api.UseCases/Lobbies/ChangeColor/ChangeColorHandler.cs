using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Lobbies.Services;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.ChangeColor;

public class ChangeColorHandler(ILobbyStore lobbies, ILobbyStateTracker tracker) : IRequestHandler<ChangeColorCommand>
{
    public async Task Handle(ChangeColorCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.UserId);
        await lobbies.Mutate(lobbyId, l => l.ChangePlayerColor(command.UserId, command.Color));
        tracker.MarkChanged(lobbyId);
    }
}
