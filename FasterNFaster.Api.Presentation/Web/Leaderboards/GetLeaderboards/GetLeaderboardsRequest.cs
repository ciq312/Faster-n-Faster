using FasterNFaster.Api.UseCases.Leaderboards.Ports;

namespace FasterNFaster.Api.Presentation.Web.Leaderboards.GetLeaderboards;

public class GetLeaderboardsRequest
{
    public LeaderboardSort Sort { get; set; } = LeaderboardSort.BestWpm;
    public bool Descending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}
