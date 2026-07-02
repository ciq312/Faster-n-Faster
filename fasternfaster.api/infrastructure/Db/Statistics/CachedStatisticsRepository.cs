using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure.Caching;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Api.Infrastructure.Db.Statistics;

public class CachedStatisticsRepository(IStatisticsRepository inner, ICache cache) : IStatisticsRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private readonly HashSet<Guid> touched = [];

    private static string Key(Guid userId) => $"stats:{userId}";

    public Task<PlayerStatistics?> GetByUserIdAsync(Guid userId) =>
        cache.GetOrSetAsync(Key(userId), () => inner.GetByUserIdAsync(userId), Ttl);

    // Part of the write path: the entity must stay EF-tracked, so it is never served from cache.
    public async Task<PlayerStatistics?> FindAsync(Guid userId)
    {
        touched.Add(userId);
        return await inner.FindAsync(userId);
    }

    public void Add(PlayerStatistics stats)
    {
        touched.Add(stats.Id);
        inner.Add(stats);
    }

    public async Task SaveAsync()
    {
        await inner.SaveAsync();
        if (touched.Count == 0) return;

        var keys = touched.Select(Key).ToArray();
        touched.Clear();

        await cache.RemoveAsync(keys);
        await cache.BumpVersionAsync(CachedLeaderboardRepository.VersionKey);
    }
}
