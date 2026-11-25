using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Races.RefreshPassage;

public class RefreshPassageHandler(
    ILobbyStore lobbies,
    PassageRefresher passageRefresher,
    ILogger<RefreshPassageHandler> logger) : IRequestHandler<RefreshPassageCommand>
{
    public async Task Handle(RefreshPassageCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.CallerId);

        var passage = await passageRefresher.FetchNext(lobbyId);

        await lobbies.Mutate(lobbyId, l => l.EnsureCanRefreshPassage(command.CallerId));
        if (!await passageRefresher.TryApply(lobbyId, passage))
        {
            throw new LobbyInRaceException("change the passage");
        }
        else
        {
            logger.LogInformation(
                "Passage changed in lobby {LobbyId} by user {UserId} {TelemetryOperation}",
                lobbyId,
                command.CallerId,
                TelemetryOperations.PassageChanged);
        }

    }
}
