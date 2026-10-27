using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Infrastructure.Leaderboards;
using FasterNFaster.Api.Infrastructure.Shared.Persistence;
using FasterNFaster.Api.UseCases.AntiCheat.Ports;
using FasterNFaster.Api.UseCases.Leaderboards.Ports;
using Shouldly;
using static FasterNFaster.IntegrationTests.StatisticsSeeder;

namespace FasterNFaster.IntegrationTests;

public class LeaderboardRepositoryTests(NoRateLimitApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    [Fact]
    public async Task SortByAvgWpm_PlayerWithoutRaces_RanksLastWithZeroAverage()
    {
        var idle = await App.SeedPlayerAsync("idle");
        var racer = await App.SeedPlayerAsync("racer", Race(60), Race(80));

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

        Assert.Equal(["Player1", "Player2"], page.Items.Select(e => e.Nick));
        Assert.Equal([10f, 20f], page.Items.Select(e => e.AvgWPM));
    }

    [Fact]
    public async Task SecondPage_ContinuesWhereFirstPageEnds()
    {
        await SeedRankedPlayersAsync();

        var page = await GetPageAsync(LeaderboardSort.BestWpm, true, 2, 2);

        Assert.Equal(5, page.TotalPlayers);
        Assert.Equal(["Player3", "Player2"], page.Items.Select(e => e.Nick));
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

        Assert.Equal(leader, Assert.Single(page.Items).Nick);
    }

    [Fact]
    public async Task BannedPlayer_ExcludedFromItemsAndTotal()
    {
        var banned = await App.SeedPlayerAsync("banned", Race(100));
        var clean = await App.SeedPlayerAsync("clean", Race(50));
        await App.ExecuteScopedAsync<IBanRepository>(repo => repo.BanAsync(banned, "cheating"));

        var page = await GetPageAsync(LeaderboardSort.BestWpm, true, 1, 10);

        Assert.Equal([clean], page.Items.Select(e => e.Id));
        Assert.Equal(1, page.TotalPlayers);
    }

    [Fact]
    public async Task ActiveSuspension_ExcludedFromItemsAndTotal()
    {
        var suspended = await App.SeedPlayerAsync("suspended", Race(100));
        var clean = await App.SeedPlayerAsync("clean", Race(50));
        await App.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(suspended, "cooldown", DateTime.UtcNow.AddHours(1)));

        var page = await GetPageAsync(LeaderboardSort.BestWpm, true, 1, 10);

        Assert.Equal([clean], page.Items.Select(e => e.Id));
        Assert.Equal(1, page.TotalPlayers);
    }

    [Fact]
    public async Task ExpiredSuspension_PlayerIncluded()
    {
        var player = await App.SeedPlayerAsync("returned", Race(50));
        await App.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(player, "cooldown", DateTime.UtcNow.AddSeconds(-1)));

        var page = await GetPageAsync(LeaderboardSort.BestWpm, true, 1, 10);

        Assert.Equal([player], page.Items.Select(e => e.Id));
        Assert.Equal(1, page.TotalPlayers);
    }

    public static TheoryData<LeaderboardSort, bool> EveryOrder()
    {
        var data = new TheoryData<LeaderboardSort, bool>();
        foreach (var sort in Enum.GetValues<LeaderboardSort>())
        {
            data.Add(sort, true);
            data.Add(sort, false);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(EveryOrder))]
    public async Task PlayerRank_EqualsListPosition_ForEverySortAndDirection(LeaderboardSort sort, bool descending)
    {
        await SeedTieHeavyPlayersAsync();

        var page = await GetPageAsync(sort, descending, 1, 100);

        page.Items.Count.ShouldBe(8);
        var ranks = await RanksOfAsync(page.Items.Select(e => e.Id), sort, descending);
        ranks.Select(r => r.Rank).ShouldBe(Enumerable.Range(1, 8));
        ranks.Select(r => r.Entry).ShouldBe(page.Items);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PlayerRank_ComputedAverageTies_OrderedById(bool descending)
    {
        var sameAverage = await SeedSameAverageWpmPlayersAsync();

        var ranks = await RanksOfAsync(sameAverage.Order(), LeaderboardSort.AvgWpm, descending);

        ranks.Select(r => r.Rank).ShouldBe([1, 2, 3]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PlayerRank_TiedValues_OrderedById(bool descending)
    {
        var ids = await SeedPlayersWithoutRacesAsync(4);

        var ranks = await RanksOfAsync(ids.Order(), LeaderboardSort.RacesTyped, descending);

        ranks.Select(r => r.Rank).ShouldBe([1, 2, 3, 4]);
    }

    [Fact]
    public async Task PlayerRank_ReturnsPlayersOwnStats()
    {
        await App.SeedPlayerAsync("other", Race(40));
        var me = await App.SeedPlayerAsync("me", Race(60, 90, 20, 1), Race(80, 70, 30, 2));

        var rank = await GetRankAsync(me, LeaderboardSort.BestWpm, true);

        rank.ShouldNotBeNull();
        rank.Rank.ShouldBe(1);
        rank.Entry.ShouldBe(new LeaderboardEntry(me, "me", 80, 90, 70, 80, 1, 50, 2));
    }

    [Fact]
    public async Task PlayerRank_BannedPlayerAhead_NotCounted()
    {
        var banned = await App.SeedPlayerAsync("banned", Race(100));
        var me = await App.SeedPlayerAsync("me", Race(50));
        await App.ExecuteScopedAsync<IBanRepository>(repo => repo.BanAsync(banned, "cheating"));

        var rank = await GetRankAsync(me, LeaderboardSort.BestWpm, true);

        rank.ShouldNotBeNull();
        rank.Rank.ShouldBe(1);
    }

    [Fact]
    public async Task PlayerRank_BannedPlayer_ReturnsNull()
    {
        var banned = await App.SeedPlayerAsync("banned", Race(100));
        await App.ExecuteScopedAsync<IBanRepository>(repo => repo.BanAsync(banned, "cheating"));

        var rank = await GetRankAsync(banned, LeaderboardSort.BestWpm, true);

        rank.ShouldBeNull();
    }

    [Fact]
    public async Task PlayerRank_ActiveSuspension_ReturnsNull()
    {
        var suspended = await App.SeedPlayerAsync("suspended", Race(100));
        await App.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(suspended, "cooldown", DateTime.UtcNow.AddHours(1)));

        var rank = await GetRankAsync(suspended, LeaderboardSort.BestWpm, true);

        rank.ShouldBeNull();
    }

    [Fact]
    public async Task PlayerRank_ExpiredSuspension_Ranked()
    {
        var player = await App.SeedPlayerAsync("returned", Race(50));
        await App.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(player, "cooldown", DateTime.UtcNow.AddSeconds(-1)));

        var rank = await GetRankAsync(player, LeaderboardSort.BestWpm, true);

        rank.ShouldNotBeNull();
        rank.Rank.ShouldBe(1);
    }

    [Fact]
    public async Task PlayerRank_NoStatistics_ReturnsNull()
    {
        var user = new User("fresh");
        await App.ExecuteScopedAsync<AppDbContext>(async context =>
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        });

        var rank = await GetRankAsync(user.Id, LeaderboardSort.BestWpm, true);

        rank.ShouldBeNull();
    }

    private Task<LeaderboardPage> GetPageAsync(LeaderboardSort sort, bool descending, int page, int pageSize) =>
        App.ExecuteScopedAsync<AppDbContext, LeaderboardPage>(context =>
            new LeaderboardRepository(context, TimeProvider.System).GetTopPlayersAsync(sort, descending, page, pageSize));

    private Task<PlayerRank?> GetRankAsync(Guid playerId, LeaderboardSort sort, bool descending) =>
        App.ExecuteScopedAsync<AppDbContext, PlayerRank?>(context =>
            new LeaderboardRepository(context, TimeProvider.System).GetPlayerRankAsync(playerId, sort, descending, CancellationToken.None));

    private async Task<List<PlayerRank>> RanksOfAsync(IEnumerable<Guid> ids, LeaderboardSort sort, bool descending)
    {
        var ranks = new List<PlayerRank>();
        foreach (var id in ids)
        {
            var rank = await GetRankAsync(id, sort, descending);
            rank.ShouldNotBeNull();
            ranks.Add(rank);
        }

        return ranks;
    }

    private async Task SeedTieHeavyPlayersAsync()
    {
        await SeedSameAverageWpmPlayersAsync();
        await App.SeedPlayerAsync("twinA", Race(30, 80, 50, 1));
        await App.SeedPlayerAsync("twinB", Race(30, 80, 50, 1));
        await App.SeedPlayerAsync("mixed", Race(30, 95, 20, 3), Race(5, 50, 5, 2));
        await App.SeedPlayerAsync("idleA");
        await App.SeedPlayerAsync("idleB");
    }

    // 31/3 and 62/6 are the same average reached through different race counts.
    private async Task<List<Guid>> SeedSameAverageWpmPlayersAsync() =>
    [
        await App.SeedPlayerAsync("avgA", Race(10, 90, 10, 1), Race(10, 90, 10, 2), Race(11, 90, 10, 2)),
        await App.SeedPlayerAsync("avgB", Race(11, 90, 10, 2), Race(10, 90, 10, 1), Race(10, 90, 10, 2)),
        await App.SeedPlayerAsync("avgC", Race(10, 90, 10, 2), Race(10, 90, 10, 2), Race(10, 90, 10, 2), Race(10, 90, 10, 2), Race(11, 90, 10, 2), Race(11, 90, 10, 2))
    ];

    private async Task SeedRankedPlayersAsync()
    {
        for (var i = 1; i <= 5; i++)
            await App.SeedPlayerAsync($"Player{i}", Race(i * 10, 95 - i, i * 20, i));
    }

    private async Task SeedOneLeaderPerSortAsync()
    {
        await App.SeedPlayerAsync("sprinter", Race(wpm: 120), Race(wpm: 10));
        await App.SeedPlayerAsync("steady", Race(wpm: 90), Race(wpm: 90));
        await App.SeedPlayerAsync("lucky", Race(accuracy: 100), Race(accuracy: 0));
        await App.SeedPlayerAsync("precise", Race(accuracy: 95), Race(accuracy: 95));
        await App.SeedPlayerAsync("winner", Race(finishPosition: 1), Race(finishPosition: 1));
        await App.SeedPlayerAsync("wordy", Race(wordsTyped: 500));
        await App.SeedPlayerAsync("grinder", Race(), Race(), Race());
    }

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
