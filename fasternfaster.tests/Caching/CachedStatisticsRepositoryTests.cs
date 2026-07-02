using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure.Db.Statistics;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Caching;

public class CachedStatisticsRepositoryTests
{
    private readonly FakeStatisticsRepository _inner = new();
    private readonly InMemoryCache _cache = new();
    private readonly CachedStatisticsRepository _sut;

    public CachedStatisticsRepositoryTests() => _sut = new CachedStatisticsRepository(_inner, _cache);

    [Fact]
    public async Task GetByUserIdAsync_SecondCall_ServedFromCache()
    {
        var stats = new PlayerStatistics(Guid.NewGuid());
        _inner.Seed(stats);

        await _sut.GetByUserIdAsync(stats.Id);
        await _sut.GetByUserIdAsync(stats.Id);

        Assert.Equal(1, _inner.GetByUserIdCalls);
    }

    [Fact]
    public async Task FindAsync_NeverCached_AlwaysHitsInner()
    {
        var stats = new PlayerStatistics(Guid.NewGuid());
        _inner.Seed(stats);

        await _sut.FindAsync(stats.Id);
        await _sut.FindAsync(stats.Id);

        Assert.Equal(2, _inner.FindCalls); // write path must stay EF-tracked
    }

    [Fact]
    public async Task SaveAsync_InvalidatesTouchedUserAndBumpsLeaderboardVersion()
    {
        var stats = new PlayerStatistics(Guid.NewGuid());
        _inner.Seed(stats);

        await _sut.GetByUserIdAsync(stats.Id); // caches the read
        await _sut.FindAsync(stats.Id);        // touches the user via the write path
        await _sut.SaveAsync();

        await _sut.GetByUserIdAsync(stats.Id); // should miss and re-query

        Assert.Equal(2, _inner.GetByUserIdCalls);
        Assert.Equal(1, await _cache.GetVersionAsync(CachedLeaderboardRepository.VersionKey));
    }

    [Fact]
    public async Task SaveAsync_WithoutTouchedUsers_DoesNotBumpVersion()
    {
        await _sut.SaveAsync();

        Assert.Equal(1, _inner.SaveCalls);
        Assert.Equal(0, await _cache.GetVersionAsync(CachedLeaderboardRepository.VersionKey));
    }
}
