using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Leaderboards;

public class GetLeaderboardHandler(ILeaderboardRepository leaderboardRepo) : IRequestHandler<GetLeaderboardQuery, GetLeaderboardResults>
{
    private const int MaxPageSize = 100;

    public async Task<GetLeaderboardResults> Handle(GetLeaderboardQuery command, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, command.Page);
        var pageSize = Math.Clamp(command.PageSize, 1, MaxPageSize);

        var result = await leaderboardRepo.GetTopPlayersAsync(command.Sort, command.Descending, page, pageSize);

        var firstRank = (page - 1) * pageSize + 1;
        var items = result.Items
            .Select((entry, i) => new LeaderboardResultDTO(
                firstRank + i, entry.Id, entry.PlayerName, entry.BestWPM, entry.BestAccuracy, entry.AvgWPM, entry.AvgAccuracy, entry.Wins, entry.WordsTyped, entry.RacesTyped))
            .ToList();

        var totalPages = (int)Math.Ceiling(result.TotalPlayers / (double)pageSize);

        return new GetLeaderboardResults(items, page, pageSize, result.TotalPlayers, totalPages);
    }
}
