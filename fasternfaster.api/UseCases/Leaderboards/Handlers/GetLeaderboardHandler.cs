using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Leaderboards;

public class GetLeaderboardHandler(ILeaderboardRepository leaderboardService) : IRequestHandler<GetLeaderboardCommand, GetLeaderboardResults>
{
    private const int MaxPageSize = 100;

    public async Task<GetLeaderboardResults> Handle(GetLeaderboardCommand command, CancellationToken cancellationToken)
    {
        int page = Math.Max(1, command.Page);
        int pageSize = Math.Clamp(command.PageSize, 1, MaxPageSize);

        LeaderboardPage result = await leaderboardService.GetTopPlayersAsync(command.Sort, command.Descending, page, pageSize);

        int firstRank = (page - 1) * pageSize + 1;
        var items = result.Items
            .Select((stat, i) => new LeaderboardResultDTO(
                firstRank + i, stat.Id, stat.User.Nick, stat.BestWPM, stat.BestAccuracy, stat.AvgWPM, stat.AvgAccuracy, stat.Wins, stat.WordsTyped, stat.RacesTyped))
            .ToList();

        int totalPages = (int)Math.Ceiling(result.TotalPlayers / (double)pageSize);

        return new GetLeaderboardResults(items, page, pageSize, result.TotalPlayers, totalPages);
    }
}
