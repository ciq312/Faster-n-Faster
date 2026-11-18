using FasterNFaster.Api.UseCases.Leaderboards.GetLeaderboard;
using FasterNFaster.Api.UseCases.Leaderboards.Ports;
using Moq;
using Shouldly;

namespace FasterNFaster.UnitTests.Leaderboards;

public class GetLeaderboardHandlerTests
{
    private readonly Mock<ILeaderboardRepository> repo = new();
    private readonly GetLeaderboardHandler sut;

    public GetLeaderboardHandlerTests() => sut = new GetLeaderboardHandler(repo.Object);

    [Fact]
    public async Task SecondPage_RanksContinueFromPreviousPage()
    {
        ReturnsPage(new LeaderboardPage([Entry("a"), Entry("b")], 12));

        var result = await sut.Handle(new GetLeaderboardQuery(LeaderboardSort.BestWpm, true, 2, 5), CancellationToken.None);

        result.Items.Select(i => i.Rank).ShouldBe([6, 7]);
    }

    [Fact]
    public async Task Items_CarryNick()
    {
        ReturnsPage(new LeaderboardPage([Entry("a"), Entry("b")], 2));

        var result = await sut.Handle(new GetLeaderboardQuery(LeaderboardSort.BestWpm, true, 1, 5), CancellationToken.None);

        result.Items.Select(i => i.Nick).ShouldBe(["a", "b"]);
    }

    [Fact]
    public async Task TotalPages_RoundsUp()
    {
        ReturnsPage(new LeaderboardPage([], 12));

        var result = await sut.Handle(new GetLeaderboardQuery(LeaderboardSort.BestWpm, true, 1, 5), CancellationToken.None);

        result.TotalPages.ShouldBe(3);
        result.TotalPlayers.ShouldBe(12);
    }

    private void ReturnsPage(LeaderboardPage page) =>
        repo.Setup(r => r.GetTopPlayersAsync(It.IsAny<LeaderboardSort>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(page);

    private static LeaderboardEntry Entry(string nick) =>
        new(Guid.NewGuid(), nick, 80, 0.95f, 70, 0.9f, 1, 100, 3);
}
