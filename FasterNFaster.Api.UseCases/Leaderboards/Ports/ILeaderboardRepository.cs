namespace FasterNFaster.Api.UseCases.Leaderboards.Ports;

public interface ILeaderboardRepository
{
    Task<LeaderboardPage> GetTopPlayersAsync(LeaderboardSort sort, bool descending, int page, int pageSize);
}
