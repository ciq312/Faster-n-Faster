using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Leaderboards;

namespace FasterNFaster.Tests.Fakes;

public class FakeLeaderboardRepository : ILeaderboardRepository
{
    public int Calls { get; private set; }
    public LeaderboardPage Page { get; set; } = new(new List<PlayerStatistics>(), 0);

    public Task<LeaderboardPage> GetTopPlayersAsync(LeaderboardSort sort, bool descending, int page, int pageSize)
    {
        Calls++;
        return Task.FromResult(Page);
    }
}
