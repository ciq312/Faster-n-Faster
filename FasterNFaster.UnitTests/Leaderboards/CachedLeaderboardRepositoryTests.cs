using FasterNFaster.Api.Infrastructure.Leaderboards;
using FasterNFaster.Api.UseCases.Leaderboards.Ports;
using FasterNFaster.UnitTests.Fakes;
using Moq;

namespace FasterNFaster.UnitTests.Leaderboards;

public class CachedLeaderboardRepositoryTests
{
    private readonly Mock<ILeaderboardRepository> inner = new();
    private readonly InMemoryCache cache = new();
    private readonly CachedLeaderboardRepository sut;

    public CachedLeaderboardRepositoryTests()
    {
        inner.Setup(r => r.GetTopPlayersAsync(It.IsAny<LeaderboardSort>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new LeaderboardPage([], 0));
        sut = new CachedLeaderboardRepository(inner.Object, cache);
    }

    [Fact]
    public async Task GetTopPlayersAsync_SecondCall_ServedFromCache()
    {
        await sut.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 1, 20);
        await sut.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 1, 20);

        inner.Verify(r => r.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetTopPlayersAsync_DifferentQuery_CachedSeparately()
    {
        await sut.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 1, 20);
        await sut.GetTopPlayersAsync(LeaderboardSort.Wins, true, 1, 20);

        inner.Verify(r => r.GetTopPlayersAsync(It.IsAny<LeaderboardSort>(), true, 1, 20), Times.Exactly(2));
    }

    [Fact]
    public async Task GetPlayerRankAsync_EveryCall_GoesToInner()
    {
        var playerId = Guid.NewGuid();

        await sut.GetPlayerRankAsync(playerId, LeaderboardSort.BestWpm, true, CancellationToken.None);
        await sut.GetPlayerRankAsync(playerId, LeaderboardSort.BestWpm, true, CancellationToken.None);

        inner.Verify(r => r.GetPlayerRankAsync(playerId, LeaderboardSort.BestWpm, true, CancellationToken.None), Times.Exactly(2));
    }
}
