using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Services;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Tests.Services;

public class LeaderboardServiceTests
{
    [Fact]
    public async Task GetTopPlayersAsync_ReturnsSortedPlayers()
    {
        // 1. Create unique options for this test run
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "LeaderboardTest")
            .Options;

        // 2. Seed the data
        List<PlayerStatistics> statistics = new List<PlayerStatistics>();

        for (int i = 1; i <= 5; i++)
        {
            PlayerStatistics stat = new PlayerStatistics(Guid.NewGuid());
            RaceParticipantResult result = new RaceParticipantResult(Guid.TryParse(i.ToString(), out Guid id) ? id : Guid.NewGuid(), stat.Id, $"Player{i}", i * 10, 95 - i, i * 5, i * 20, i);
            stat.RegisterRace(result);
            statistics.Add(stat);
        }
        using (var context = new AppDbContext(options))
        {
            context.Statistics.AddRange(statistics);
            await context.SaveChangesAsync();
        }

        // 3. Run the test
        using (var context = new AppDbContext(options))
        {
            var service = new LeaderboardService(context);

            // Act: Sort by Score, Descending, take top 2
            var result = await service.GetTopPlayersAsync("AvgWPM", true, 2);

            // Assert
            var list = result.ToList();
            Assert.Equal(2, list.Count);
            Assert.Equal(50, list[0].AvgWPM);
            Assert.Equal(40, list[1].AvgWPM);

            var resultAcc = await service.GetTopPlayersAsync("AvgAccuracy", true, 3);
            var listAcc = resultAcc.ToList();

            Assert.Equal(3, listAcc.Count);
            Assert.Equal(94, listAcc[0].AvgAccuracy);
            Assert.Equal(93, listAcc[1].AvgAccuracy);
            Assert.Equal(92, listAcc[2].AvgAccuracy);
        }
    }
}