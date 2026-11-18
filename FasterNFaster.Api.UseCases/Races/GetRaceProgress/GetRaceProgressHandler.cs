using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Races.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Races.GetRaceProgress;

public class GetRaceProgressHandler(IRaceStore races, ILobbyStore lobbies) : IRequestHandler<GetRaceProgressQuery, RaceProgressDTO?>
{
    public async Task<RaceProgressDTO?> Handle(GetRaceProgressQuery query, CancellationToken cancellationToken)
    {
        if (lobbies.GetLobbyIdOfPlayer(query.UserId) is not Guid lobbyId) return null;

        return await races.TryRead(lobbyId, r =>
            r.Participants.GetValueOrDefault(query.UserId) is { } participant
                ? new RaceProgressDTO(participant.LastValidIndex, participant.WrongTail, participant.Mistakes)
                : null);
    }
}
