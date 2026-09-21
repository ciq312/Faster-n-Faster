using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Leaderboards;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Handlers;

public class GetLeaderboardHandlerTests
{
    private readonly FakeLeaderboardRepository _repo = new();
    private readonly GetLeaderboardHandler _sut;

    public GetLeaderboardHandlerTests() => _sut = new GetLeaderboardHandler(_repo);

    [Fact]
    public async Task SecondPage_RanksContinueFromPreviousPage()
    {
        _repo.Page = new LeaderboardPage([Entry("a"), Entry("b")], 12);

        var result = await _sut.Handle(new GetLeaderboardQuery(LeaderboardSort.BestWpm, true, 2, 5), CancellationToken.None);

        Assert.Equal(new[] { 6, 7 }, result.Items.Select(i => i.Rank));
        Assert.Equal(new[] { "a", "b" }, result.Items.Select(i => i.PlayerName));
    }

    [Fact]
    public async Task TotalPages_RoundsUp()
    {
        _repo.Page = new LeaderboardPage([], 12);

        var result = await _sut.Handle(new GetLeaderboardQuery(LeaderboardSort.BestWpm, true, 1, 5), CancellationToken.None);

        Assert.Equal(3, result.TotalPages);
        Assert.Equal(12, result.TotalPlayers);
    }

    private static LeaderboardEntry Entry(string name) =>
        new(Guid.NewGuid(), name, 80, 0.95f, 70, 0.9f, 1, 100, 3);
}
