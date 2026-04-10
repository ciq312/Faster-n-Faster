using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Leaderboards.Handlers;

public class GetLeaderboardHandler(ILeaderboardService leaderboardService) : IHandler<GetLeaderboardCommand, GetLeaderboardResults>
{
    private readonly ILeaderboardService leaderboardService = leaderboardService;

    public async Task<GetLeaderboardResults> Handle(GetLeaderboardCommand command)
    {
        IEnumerable<PlayerStatistics> topPlayers = await leaderboardService.GetTopPlayersAsync(command.Criteria, command.IsDescending, command.PlayersCount);

        var results = topPlayers.Select(stat => new LeaderboardResultDTO(stat.Id, stat.User.Nick, stat.BestWPM, stat.BestAccuracy, stat.AvgWPM, stat.AvgAccuracy, stat.Wins, stat.WordsTyped, stat.RacesTyped)) ?? Enumerable.Empty<LeaderboardResultDTO>();



        return new GetLeaderboardResults(results.ToList());
    }
}


