using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.UseCases.Leaderboards.GetLeaderboard;

namespace FasterNFaster.Api.Presentation.Web.Leaderboards.GetMyLeaderboardRank;

public class GetMyLeaderboardRankSummary : ApiSummary<GetMyLeaderboardRankEndpoint>
{
    public GetMyLeaderboardRankSummary()
    {
        Summary = "Get the current player's leaderboard row";
        Description = "Returns the caller's rank and stats for `sort` and `descending`, ranked exactly like `GET /api/leaderboards` (ties broken by id). Not cached.";
        Response<LeaderboardResultDTO>(200, "The player's row", example: new LeaderboardResultDTO(
            57, Guid.Parse("3f2b8c1e-7a4d-4e9b-9c2a-1d5e6f7a8b9c"), "player1", 112.4, 98.1, 95.7, 96.3, 14, 5210, 42));
        ValidationFailed();
        Response<ApiErrorResponse>(401, "Not logged in (`not_authenticated`)");
        Response<ApiErrorResponse>(403, "Not a registered player, e.g. a guest (`forbidden`)");
        Response<ApiErrorResponse>(404, "The player has no leaderboard row: no statistics yet, banned or suspended (`not_on_leaderboard`)");
    }
}
