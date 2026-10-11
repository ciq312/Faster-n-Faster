using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Infrastructure.Db;
using FasterNFaster.Api.Infrastructure.Db.Statistics;
using FasterNFaster.Api.UseCases.Leaderboards;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Tests.Services;

public class LeaderboardServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    public async Task InitializeAsync()
    {
        var users = new List<User>();
        var statistics = new List<PlayerStatistics>();

        for (var i = 1; i <= 5; i++)
        {
            var user = new User($"Player{i}");
            var stat = new PlayerStatistics(user.Id);
            var result = new RaceParticipantResult(Guid.NewGuid(), stat.Id, user.Nick, i * 10, 95 - i, i * 5, i * 20, i);
            stat.RegisterRace(result);
            users.Add(user);
            statistics.Add(stat);
        }

        using var context = new AppDbContext(options);
        context.Users.AddRange(users);
        context.Statistics.AddRange(statistics);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetTopPlayersAsync_ReturnsSortedPlayers()
    {

        using var context = new AppDbContext(options);
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

        using var context = new AppDbContext(options);
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
        using var context = new AppDbContext(options);
        var service = new LeaderboardRepository(context);

        var page2 = await service.GetTopPlayersAsync(LeaderboardSort.BestWpm, true, 2, 2);
        var list = page2.Items;
        Assert.Equal(5, page2.TotalPlayers);
        Assert.Equal(2, list.Count);
        // Page 1 holds WPM 50 and 40; page 2 continues at 30, 20.
        Assert.Equal(30, list[0].AvgWPM);
        Assert.Equal(20, list[1].AvgWPM);
    }

    [Fact]
    public async Task TiedValues_PagesOrderTiesById()
    {
        var ids = await SeedPlayersWithoutRacesAsync(4);
        using var context = new AppDbContext(options);
        var repository = new LeaderboardRepository(context);

        var page1 = await repository.GetTopPlayersAsync(LeaderboardSort.RacesTyped, false, 1, 2);
        var page2 = await repository.GetTopPlayersAsync(LeaderboardSort.RacesTyped, false, 2, 2);

        Assert.Equal(ids.Order(), page1.Items.Concat(page2.Items).Select(e => e.Id));
    }

    [Fact]
    public async Task SortByAvgWpm_UsesAverageNotBest()
    {
        using (var seedContext = new AppDbContext(options))
        {
            var player1 = await seedContext.Statistics.Include(s => s.User).SingleAsync(s => s.User.Nick == "Player1");
            player1.RegisterRace(new RaceParticipantResult(Guid.NewGuid(), player1.Id, "Player1", 75, 90, 0, 10, 2));
            await seedContext.SaveChangesAsync();
        }

        using var context = new AppDbContext(options);
        var repository = new LeaderboardRepository(context);

        var result = await repository.GetTopPlayersAsync(LeaderboardSort.AvgWpm, true, 1, 3);

        Assert.Equal(["Player5", "Player1", "Player4"], result.Items.Select(e => e.PlayerName));
        Assert.Equal(42.5f, result.Items[1].AvgWPM);
    }

    private async Task<List<Guid>> SeedPlayersWithoutRacesAsync(int count)
    {
        var users = Enumerable.Range(0, count)
            .Select(i => new User($"Idle{i}"))
            .OrderByDescending(u => u.Id)
            .ToList();

        using var context = new AppDbContext(options);
        context.Users.AddRange(users);
        context.Statistics.AddRange(users.Select(u => new PlayerStatistics(u.Id)));
        await context.SaveChangesAsync();

        return users.Select(u => u.Id).ToList();
    }
}
