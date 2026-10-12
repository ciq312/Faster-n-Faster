using FasterNFaster.Api.UseCases.Lobbies.Services;
using MediatR;

namespace FasterNFaster.Api.UseCases.Races.RefreshPassage;

public class RefreshPassageHandler(
    ILobbyAccess lobbies,
    PassageRefresher passageRefresher) : IRequestHandler<RefreshPassageCommand>
{
    public async Task Handle(RefreshPassageCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.CallerId);

        await lobbies.Mutate(lobbyId, l => l.EnsureCanRefreshPassage(command.CallerId));

        await passageRefresher.Refresh(lobbyId);
    }
}
