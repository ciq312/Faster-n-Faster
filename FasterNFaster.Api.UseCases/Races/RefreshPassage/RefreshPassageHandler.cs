using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Races.RefreshPassage;

public class RefreshPassageHandler(
    ILobbyStore lobbies,
    PassageRefresher passageRefresher) : IRequestHandler<RefreshPassageCommand>
{
    public async Task Handle(RefreshPassageCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.CallerId);

        var passage = await passageRefresher.FetchNext(lobbyId);

        await lobbies.Mutate(lobbyId, l => l.EnsureCanRefreshPassage(command.CallerId));
        if (!await passageRefresher.TryApply(lobbyId, passage))
            throw new LobbyInRaceException("change the passage");
    }
}
