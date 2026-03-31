using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Leaderboards.Handlers;

public class GetLeaderboardHandler(ILeaderboardService leaderboardService) : IHandler<GetLeaderboardCommand, GetLeaderboardResults>
{
    private readonly ILeaderboardService _leaderboardService = leaderboardService;

    public async Task<GetLeaderboardResults> Handle(GetLeaderboardCommand command)
    {
        IEnumerable<PlayerStatistics> topPlayers = await _leaderboardService.GetTopPlayersAsync(command.Criteria, command.IsDescending, command.PlayersCount);

        GetLeaderboardResults results = new GetLeaderboardResults(topPlayers.Select(stat => new LeaderboardResultDTO(stat.Id, stat.User.Nick, stat.BestWPM, stat.BestAccuracy, stat.Wins, stat.WordsTyped, stat.RacesTyped)).ToList());

        return results;
    }
}


