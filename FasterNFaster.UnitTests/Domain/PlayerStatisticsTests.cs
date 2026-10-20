using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races;

namespace FasterNFaster.UnitTests.Domain;

public class PlayerStatisticsTests
{
    private static RaceParticipantResult Result(float wpm, float accuracy) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "alice", wpm, accuracy, 0, 10, 1, DateTimeOffset.UnixEpoch);

    [Fact]
    public void NoRacesRegistered_AveragesAreZero()
    {
        var statistics = new PlayerStatistics(Guid.NewGuid());

        Assert.Equal(0, statistics.AvgWPM);
        Assert.Equal(0, statistics.AvgAccuracy);
    }

    [Fact]
    public void TwoRaces_AveragesAreSumsOverRaceCount()
    {
        var statistics = new PlayerStatistics(Guid.NewGuid());

        statistics.RegisterRace(Result(60, 90));
        statistics.RegisterRace(Result(80, 100));

        Assert.Equal(70, statistics.AvgWPM);
        Assert.Equal(95, statistics.AvgAccuracy);
    }

    [Fact]
    public void TwoRaces_BestKeepsMaximum()
    {
        var statistics = new PlayerStatistics(Guid.NewGuid());

        statistics.RegisterRace(Result(80, 100));
        statistics.RegisterRace(Result(60, 90));

        Assert.Equal(80, statistics.BestWPM);
        Assert.Equal(100, statistics.BestAccuracy);
    }
}
