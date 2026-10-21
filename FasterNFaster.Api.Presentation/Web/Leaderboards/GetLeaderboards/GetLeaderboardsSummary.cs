using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.UseCases.Leaderboards.GetLeaderboard;

namespace FasterNFaster.Api.Presentation.Web.Leaderboards.GetLeaderboards;

public class GetLeaderboardsSummary : ApiSummary<GetLeaderboardsEndpoint>
{
    public GetLeaderboardsSummary()
    {
        Summary = "Get a leaderboard page";
        Description = "Ranks registered players by `sort` (best WPM by default), `pageSize` players per page.";
        Response<GetLeaderboardResults>(200, "The page", example: new GetLeaderboardResults(
            [new LeaderboardResultDTO(1, Guid.Parse("3f2b8c1e-7a4d-4e9b-9c2a-1d5e6f7a8b9c"), "player1", 112.4, 98.1, 95.7, 96.3, 14, 5210, 42)],
            Page: 1,
            PageSize: 30,
            TotalPlayers: 1,
            TotalPages: 1));
    }
}
