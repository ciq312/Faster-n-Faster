using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Races.Services;
using MediatR;

namespace FasterNFaster.Api.UseCases.Races.RefreshPassage;

public class RefreshPassageHandler(
    ILobbyAccess lobbies,
    IRaceAccess races) : IRequestHandler<RefreshPassageCommand>
{
    public async Task Handle(RefreshPassageCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.CallerId);

        await lobbies.Mutate(lobbyId, l => l.EnsureCanRefreshPassage(command.CallerId));

        await races.RefreshPassage(lobbyId);
    }
}
