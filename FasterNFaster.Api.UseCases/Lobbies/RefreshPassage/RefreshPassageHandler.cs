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
        var lobby = lobbies.GetOfPlayerRequired(command.CallerId);

        lobby.EnsureCanRefreshPassage(command.CallerId);

        await races.RefreshPassage(lobby.Id);
    }
}
