using FasterNFaster.Api.Infrastructure.Db.Statistics;
using FasterNFaster.Api.UseCases.Leaderboards;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Caching;

public class CachedLeaderboardRepositoryTests
{
    private readonly FakeLeaderboardRepository _inner = new();
    private readonly InMemoryCache _cache = new();
    private readonly CachedLeaderboardRepository _sut;

    public CachedLeaderboardRepositoryTests() => _sut = new CachedLeaderboardRepository(_inner, _cache);

    [Fact]
    public async Task GetTopPlayersAsync_SecondCall_SameVersion_ServedFromCache()
    {
        await _sut.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 1, 20);
        await _sut.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 1, 20);

        Assert.Equal(1, _inner.Calls);
    }

    [Fact]
    public async Task GetTopPlayersAsync_AfterVersionBump_Refetches()
    {
        await _sut.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 1, 20);

        await _cache.BumpVersionAsync(CachedLeaderboardRepository.VersionKey);
        await _sut.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 1, 20);

        Assert.Equal(2, _inner.Calls);
    }

    [Fact]
    public async Task GetTopPlayersAsync_DifferentQuery_CachedSeparately()
    {
        await _sut.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 1, 20);
        await _sut.GetTopPlayersAsync(LeaderboardSort.Wins, true, 1, 20);

        Assert.Equal(2, _inner.Calls);
    }
}
