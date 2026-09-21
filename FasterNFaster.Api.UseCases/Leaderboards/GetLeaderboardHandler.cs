using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Leaderboards;

public class GetLeaderboardHandler(ILeaderboardRepository leaderboardRepo) : IRequestHandler<GetLeaderboardQuery, GetLeaderboardResults>
{
    private const int MaxPageSize = 100;

    public async Task<GetLeaderboardResults> Handle(GetLeaderboardQuery command, CancellationToken cancellationToken)
    {
        int page = Math.Max(1, command.Page);
        int pageSize = Math.Clamp(command.PageSize, 1, MaxPageSize);

        LeaderboardPage result = await leaderboardRepo.GetTopPlayersAsync(command.Sort, command.Descending, page, pageSize);

        int firstRank = (page - 1) * pageSize + 1;
        var items = result.Items
            .Select((entry, i) => new LeaderboardResultDTO(
                firstRank + i, entry.Id, entry.PlayerName, entry.BestWPM, entry.BestAccuracy, entry.AvgWPM, entry.AvgAccuracy, entry.Wins, entry.WordsTyped, entry.RacesTyped))
            .ToList();

        int totalPages = (int)Math.Ceiling(result.TotalPlayers / (double)pageSize);

        return new GetLeaderboardResults(items, page, pageSize, result.TotalPlayers, totalPages);
    }
}
