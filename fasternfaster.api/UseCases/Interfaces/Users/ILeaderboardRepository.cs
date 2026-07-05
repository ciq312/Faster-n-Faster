using FasterNFaster.Api.UseCases.Leaderboards;

namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface ILeaderboardRepository
{
    Task<LeaderboardPage> GetTopPlayersAsync(LeaderboardSort sort, bool descending, int page, int pageSize);
}
