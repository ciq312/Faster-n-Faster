using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.UseCases.Leaderboards.Exceptions;
using FasterNFaster.Api.UseCases.Leaderboards.GetMyLeaderboardRank;
using FasterNFaster.Api.UseCases.Leaderboards.Ports;
using Moq;
using Shouldly;

namespace FasterNFaster.UnitTests.Leaderboards;

public class GetMyLeaderboardRankHandlerTests
{
    private readonly Mock<ILeaderboardRepository> repo = new();
    private readonly GetMyLeaderboardRankHandler sut;

    public GetMyLeaderboardRankHandlerTests() => sut = new GetMyLeaderboardRankHandler(repo.Object);

    [Fact]
    public async Task PlayerOnLeaderboard_ReturnsRankAndStats()
    {
        var entry = new LeaderboardEntry(Guid.NewGuid(), "me", 80, 0.95f, 70, 0.9f, 4, 100, 3);
        repo.Setup(r => r.GetPlayerRankAsync(entry.Id, It.IsAny<LeaderboardSort>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRank(57, entry));

        var result = await sut.Handle(new GetMyLeaderboardRankQuery(entry.Id, LeaderboardSort.BestWpm, true), CancellationToken.None);

        result.Rank.ShouldBe(57);
        result.Id.ShouldBe(entry.Id);
        result.Nick.ShouldBe("me");
        result.Wins.ShouldBe(4);
    }

    [Fact]
    public async Task AnySortAndDirection_PassedToRepository()
    {
        var playerId = Guid.NewGuid();
        repo.Setup(r => r.GetPlayerRankAsync(playerId, LeaderboardSort.Wins, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRank(1, new LeaderboardEntry(playerId, "me", 0, 0, 0, 0, 0, 0, 0)));

        await sut.Handle(new GetMyLeaderboardRankQuery(playerId, LeaderboardSort.Wins, false), CancellationToken.None);

        repo.Verify(r => r.GetPlayerRankAsync(playerId, LeaderboardSort.Wins, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlayerNotOnLeaderboard_ThrowsNotOnLeaderboard()
    {
        repo.Setup(r => r.GetPlayerRankAsync(It.IsAny<Guid>(), It.IsAny<LeaderboardSort>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerRank?)null);

        await Should.ThrowAsync<NotOnLeaderboardException>(() =>
            sut.Handle(new GetMyLeaderboardRankQuery(Guid.NewGuid(), LeaderboardSort.BestWpm, true), CancellationToken.None));
    }

    [Fact]
    public void NotOnLeaderboard_HasNotOnLeaderboardCode()
    {
        new NotOnLeaderboardException().Code.ShouldBe(ErrorCodes.NotOnLeaderboard);
    }
}
