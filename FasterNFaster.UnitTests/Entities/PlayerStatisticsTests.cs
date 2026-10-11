using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Races;

namespace FasterNFaster.Tests.Entities;

public class PlayerStatisticsTests
{
    private static RaceParticipantResult Result(float wpm, float accuracy) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "alice", wpm, accuracy, 0, 10, 1);

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
