using FasterNFaster.Api.Infrastructure.Leaderboards;
using FasterNFaster.Api.UseCases.Leaderboards.Ports;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Leaderboards;

public class CachedLeaderboardRepositoryTests
{
    private readonly FakeLeaderboardRepository inner = new();
    private readonly InMemoryCache cache = new();
    private readonly CachedLeaderboardRepository sut;

    public CachedLeaderboardRepositoryTests() => sut = new CachedLeaderboardRepository(inner, cache);

    [Fact]
    public async Task GetTopPlayersAsync_SecondCall_ServedFromCache()
    {
        await sut.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 1, 20);
        await sut.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 1, 20);

        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task GetTopPlayersAsync_DifferentQuery_CachedSeparately()
    {
        await sut.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 1, 20);
        await sut.GetTopPlayersAsync(LeaderboardSort.Wins, true, 1, 20);

        Assert.Equal(2, inner.Calls);
    }
}
