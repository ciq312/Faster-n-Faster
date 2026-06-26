using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Leaderboards.Handlers;

public class GetLeaderboardHandler(ILeaderboardService leaderboardService) : IRequestHandler<GetLeaderboardCommand, GetLeaderboardResults>
{
    public async Task<GetLeaderboardResults> Handle(GetLeaderboardCommand command, CancellationToken cancellationToken)
    {
        IEnumerable<PlayerStatistics> topPlayers = await leaderboardService.GetTopPlayersAsync(command.Criteria, command.IsDescending, command.PlayersCount);

        var results = topPlayers
            .Select(stat => new LeaderboardResultDTO(stat.Id, stat.User.Nick, stat.BestWPM, stat.BestAccuracy, stat.AvgWPM, stat.AvgAccuracy, stat.Wins, stat.WordsTyped, stat.RacesTyped))
            .ToList();

        return new GetLeaderboardResults(results);
    }
}
