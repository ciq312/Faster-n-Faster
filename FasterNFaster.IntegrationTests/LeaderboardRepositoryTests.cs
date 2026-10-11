using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Infrastructure.Db;
using FasterNFaster.Api.Infrastructure.Db.Statistics;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Leaderboards;

namespace FasterNFaster.IntegrationTests;

public class LeaderboardRepositoryTests(NoRateLimitApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    [Fact]
    public async Task SortByAvgWpm_PlayerWithoutRaces_RanksLastWithZeroAverage()
    {
        var idle = await SeedPlayerAsync("idle");
        var racer = await SeedPlayerAsync("racer", Race(60), Race(80));

        var page = await GetPageAsync(LeaderboardSort.AvgWpm, true, 1, 10);

        Assert.Equal([racer, idle], page.Items.Select(e => e.Id));
        Assert.Equal(70, page.Items[0].AvgWPM);
        Assert.Equal(0, page.Items[1].AvgWPM);
    }

    [Fact]
    public async Task SortByAvgWpm_Ascending_ReturnsLowestFirst()
    {
        await SeedRankedPlayersAsync();

        var page = await GetPageAsync(LeaderboardSort.AvgWpm, false, 1, 2);

        Assert.Equal(["Player1", "Player2"], page.Items.Select(e => e.PlayerName));
        Assert.Equal([10f, 20f], page.Items.Select(e => e.AvgWPM));
    }

    [Fact]
    public async Task SecondPage_ContinuesWhereFirstPageEnds()
    {
        await SeedRankedPlayersAsync();

        var page = await GetPageAsync(LeaderboardSort.BestWpm, true, 2, 2);

        Assert.Equal(5, page.TotalPlayers);
        Assert.Equal(["Player3", "Player2"], page.Items.Select(e => e.PlayerName));
        Assert.Equal([30f, 20f], page.Items.Select(e => e.BestWPM));
    }

    [Fact]
    public async Task TiedValues_PagesOrderTiesById()
    {
        var ids = await SeedPlayersWithoutRacesAsync(4);

        var page1 = await GetPageAsync(LeaderboardSort.RacesTyped, false, 1, 2);
        var page2 = await GetPageAsync(LeaderboardSort.RacesTyped, false, 2, 2);

        Assert.Equal(ids.Order(), page1.Items.Concat(page2.Items).Select(e => e.Id));
    }

    [Theory]
    [InlineData(LeaderboardSort.BestWpm, "sprinter")]
    [InlineData(LeaderboardSort.AvgWpm, "steady")]
    [InlineData(LeaderboardSort.BestAccuracy, "lucky")]
    [InlineData(LeaderboardSort.AvgAccuracy, "precise")]
    [InlineData(LeaderboardSort.Wins, "winner")]
    [InlineData(LeaderboardSort.WordsTyped, "wordy")]
    [InlineData(LeaderboardSort.RacesTyped, "grinder")]
    public async Task EachSort_Descending_RanksThatColumnsLeaderFirst(LeaderboardSort sort, string leader)
    {
        await SeedOneLeaderPerSortAsync();

        var page = await GetPageAsync(sort, true, 1, 1);

        Assert.Equal(leader, Assert.Single(page.Items).PlayerName);
    }

    [Fact]
    public async Task BannedPlayer_ExcludedFromItemsAndTotal()
    {
        var banned = await SeedPlayerAsync("banned", Race(100));
        var clean = await SeedPlayerAsync("clean", Race(50));
        await App.ExecuteScopedAsync<IBanRepository>(repo => repo.BanAsync(banned, "cheating"));

        var page = await GetPageAsync(LeaderboardSort.BestWpm, true, 1, 10);

        Assert.Equal([clean], page.Items.Select(e => e.Id));
        Assert.Equal(1, page.TotalPlayers);
    }

    [Fact]
    public async Task ActiveSuspension_ExcludedFromItemsAndTotal()
    {
        var suspended = await SeedPlayerAsync("suspended", Race(100));
        var clean = await SeedPlayerAsync("clean", Race(50));
        await App.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(suspended, "cooldown", DateTime.UtcNow.AddHours(1)));

        var page = await GetPageAsync(LeaderboardSort.BestWpm, true, 1, 10);

        Assert.Equal([clean], page.Items.Select(e => e.Id));
        Assert.Equal(1, page.TotalPlayers);
    }

    [Fact]
    public async Task ExpiredSuspension_PlayerIncluded()
    {
        var player = await SeedPlayerAsync("returned", Race(50));
        await App.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(player, "cooldown", DateTime.UtcNow.AddSeconds(-1)));

        var page = await GetPageAsync(LeaderboardSort.BestWpm, true, 1, 10);

        Assert.Equal([player], page.Items.Select(e => e.Id));
        Assert.Equal(1, page.TotalPlayers);
    }

    private static RaceParticipantResult Race(float wpm = 30, float accuracy = 60, int wordsTyped = 10, int? finishPosition = 2) =>
        new(Guid.NewGuid(), Guid.Empty, "racer", wpm, accuracy, 0, wordsTyped, finishPosition);

    private Task<LeaderboardPage> GetPageAsync(LeaderboardSort sort, bool descending, int page, int pageSize) =>
        App.ExecuteScopedAsync<AppDbContext, LeaderboardPage>(context =>
            new LeaderboardRepository(context).GetTopPlayersAsync(sort, descending, page, pageSize));

    private async Task SeedRankedPlayersAsync()
    {
        for (var i = 1; i <= 5; i++)
            await SeedPlayerAsync($"Player{i}", Race(i * 10, 95 - i, i * 20, i));
    }

    private async Task SeedOneLeaderPerSortAsync()
    {
        await SeedPlayerAsync("sprinter", Race(wpm: 120), Race(wpm: 10));
        await SeedPlayerAsync("steady", Race(wpm: 90), Race(wpm: 90));
        await SeedPlayerAsync("lucky", Race(accuracy: 100), Race(accuracy: 0));
        await SeedPlayerAsync("precise", Race(accuracy: 95), Race(accuracy: 95));
        await SeedPlayerAsync("winner", Race(finishPosition: 1), Race(finishPosition: 1));
        await SeedPlayerAsync("wordy", Race(wordsTyped: 500));
        await SeedPlayerAsync("grinder", Race(), Race(), Race());
    }

    private Task<Guid> SeedPlayerAsync(string nick, params RaceParticipantResult[] races) =>
        App.ExecuteScopedAsync<AppDbContext, Guid>(async context =>
        {
            var user = new User(nick);
            var statistics = new PlayerStatistics(user.Id);
            foreach (var race in races)
                statistics.RegisterRace(race);

            context.Users.Add(user);
            context.Statistics.Add(statistics);
            await context.SaveChangesAsync();
            return user.Id;
        });

    private async Task<List<Guid>> SeedPlayersWithoutRacesAsync(int count)
    {
        var users = Enumerable.Range(0, count)
            .Select(i => new User($"Idle{i}"))
            .OrderByDescending(u => u.Id)
            .ToList();

        // One save per user: EF Core sorts a batch's inserts by key, which would store rows in id order
        // and let a missing id tie-break go unnoticed.
        foreach (var user in users)
        {
            await App.ExecuteScopedAsync<AppDbContext>(async context =>
            {
                context.Users.Add(user);
                context.Statistics.Add(new PlayerStatistics(user.Id));
                await context.SaveChangesAsync();
            });
        }

        return users.Select(u => u.Id).ToList();
    }
}
