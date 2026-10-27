using FasterNFaster.Api.UseCases.Leaderboards.Ports;

namespace FasterNFaster.Api.Presentation.Web.Leaderboards.GetMyLeaderboardRank;

public class GetMyLeaderboardRankRequest
{
    public LeaderboardSort Sort { get; set; } = LeaderboardSort.BestWpm;
    public bool Descending { get; set; } = true;
}
