using FasterNFaster.Api.Infrastructure.Caching;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Leaderboards;

namespace FasterNFaster.Api.Infrastructure.Db.Statistics;

public class CachedLeaderboardRepository(ILeaderboardRepository inner, ICache cache) : ILeaderboardRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
    public const string VersionKey = "lb:version";

    public async Task<LeaderboardPage> GetTopPlayersAsync(LeaderboardSort sort, bool descending, int page, int pageSize)
    {
        long version = await cache.GetVersionAsync(VersionKey);
        string key = $"lb:v{version}:{sort}:{descending}:{page}:{pageSize}";

        return (await cache.GetOrSetAsync<LeaderboardPage>(
            key, () => inner.GetTopPlayersAsync(sort, descending, page, pageSize)!, Ttl))!;
    }
}
