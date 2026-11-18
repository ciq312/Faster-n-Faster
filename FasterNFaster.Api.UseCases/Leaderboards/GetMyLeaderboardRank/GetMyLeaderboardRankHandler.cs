using FasterNFaster.Api.UseCases.Leaderboards.Exceptions;
using FasterNFaster.Api.UseCases.Leaderboards.GetLeaderboard;
using FasterNFaster.Api.UseCases.Leaderboards.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Leaderboards.GetMyLeaderboardRank;

public class GetMyLeaderboardRankHandler(ILeaderboardRepository leaderboardRepo) : IRequestHandler<GetMyLeaderboardRankQuery, LeaderboardResultDTO>
{
    public async Task<LeaderboardResultDTO> Handle(GetMyLeaderboardRankQuery query, CancellationToken cancellationToken)
    {
        var rank = await leaderboardRepo.GetPlayerRankAsync(query.PlayerId, query.Sort, query.Descending, cancellationToken)
            ?? throw new NotOnLeaderboardException();

        return LeaderboardResultDTO.From(rank.Rank, rank.Entry);
    }
}
