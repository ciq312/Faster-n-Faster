using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.Infrastructure.Db;
using FasterNFaster.Api.Infrastructure.Db.Statistics;
using FasterNFaster.Api.Infrastructure.Users;
using FasterNFaster.Api.UseCases.Leaderboards;
using FasterNFaster.Api.UseCases.Services;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Tests.Services;

public class LeaderboardServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    public async Task InitializeAsync()
    {
        List<User> users = new List<User>();
        List<PlayerStatistics> statistics = new List<PlayerStatistics>();

        for (int i = 1; i <= 5; i++)
        {
            User user = new User($"Player{i}");
            PlayerStatistics stat = new PlayerStatistics(user.Id);
            RaceParticipantResult result = new RaceParticipantResult(Guid.NewGuid(), stat.Id, user.Nick, i * 10, 95 - i, i * 5, i * 20, i);
            stat.RegisterRace(result);
            users.Add(user);
            statistics.Add(stat);
        }

        using var context = new AppDbContext(_options);
        context.Users.AddRange(users);
        context.Statistics.AddRange(statistics);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetTopPlayersAsync_ReturnsSortedPlayers()
    {

        using var context = new AppDbContext(_options);
        var service = new LeaderboardRepository(context);

        var result = await service.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 1, 2);
        var list = result.Items;
        Assert.Equal(5, result.TotalPlayers);
        Assert.Equal(2, list.Count);
        Assert.Equal(50, list[0].AvgWPM);
        Assert.Equal(40, list[1].AvgWPM);

        var resultAcc = await service.GetTopPlayersAsync(LeaderboardSort.AvgAccuracy, true, 1, 3);
        var listAcc = resultAcc.Items;
        Assert.Equal(3, listAcc.Count);
        Assert.Equal(94, listAcc[0].AvgAccuracy);
        Assert.Equal(93, listAcc[1].AvgAccuracy);
        Assert.Equal(92, listAcc[2].AvgAccuracy);
    }

    [Fact]
    public async Task GetTopPlayersAsync_ReturnsSortedPlayersAsc()
    {

        using var context = new AppDbContext(_options);
        var service = new LeaderboardRepository(context);

        // Ascending now means literal sort direction: page 1 holds the lowest values.
        var result = await service.GetTopPlayersAsync(LeaderboardSort.AvgWpm, false, 1, 2);
        var list = result.Items;
        Assert.Equal(2, list.Count);
        Assert.Equal(10, list[0].AvgWPM);
        Assert.Equal(20, list[1].AvgWPM);

        var resultAcc = await service.GetTopPlayersAsync(LeaderboardSort.AvgAccuracy, false, 1, 3);
        var listAcc = resultAcc.Items;
        Assert.Equal(3, listAcc.Count);
        Assert.Equal(90, listAcc[0].AvgAccuracy);
        Assert.Equal(91, listAcc[1].AvgAccuracy);
        Assert.Equal(92, listAcc[2].AvgAccuracy);
    }

    [Fact]
    public async Task GetTopPlayersAsync_SecondPage_SkipsFirstPage()
    {
        using var context = new AppDbContext(_options);
        var service = new LeaderboardRepository(context);

        var page2 = await service.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 2, 2);
        var list = page2.Items;
        Assert.Equal(5, page2.TotalPlayers);
        Assert.Equal(2, list.Count);
        // Page 1 holds WPM 50 and 40; page 2 continues at 30, 20.
        Assert.Equal(30, list[0].AvgWPM);
        Assert.Equal(20, list[1].AvgWPM);
    }
}