using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;

public class RefreshPassageHandler(
    ILobbyAccess lobbies,
    IRaceAccess races) : IRequestHandler<RefreshPassageCommand>
{
    public async Task Handle(RefreshPassageCommand command, CancellationToken cancellationToken)
    {
        Lobby lobby = lobbies.GetOfPlayerRequired(command.CallerId);

        if (lobby.IsSessionActive) throw new InvalidOperationException("Can't refresh when session active");

        lobby.ValidateHost(command.CallerId);

        await races.RefreshPassage(lobby.Id);
    }
}
