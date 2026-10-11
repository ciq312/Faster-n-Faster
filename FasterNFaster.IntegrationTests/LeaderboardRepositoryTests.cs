using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Infrastructure.Db;
using FasterNFaster.Api.Infrastructure.Db.Statistics;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Leaderboards;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FasterNFaster.IntegrationTests;

public class LeaderboardRepositoryTests(NoRateLimitApplicationFactory<Program> fixture) : IClassFixture<NoRateLimitApplicationFactory<Program>>, IAsyncLifetime
{
    private WebApplicationFactory<Program> app = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        app = fixture.CreateApp();
    }

    public async Task DisposeAsync() => await app.DisposeAsync();

    [Fact]
    public async Task SortByAvgWpm_PlayerWithoutRaces_RanksLastWithZeroAverage()
    {
        var idle = await SeedPlayerAsync("idle");
        var racer = await SeedPlayerAsync("racer", 60, 80);

        var page = await app.ExecuteScopedAsync<AppDbContext, LeaderboardPage>(context =>
            new LeaderboardRepository(context).GetTopPlayersAsync(LeaderboardSort.AvgWpm, true, 1, 10));

        Assert.Equal([racer, idle], page.Items.Select(e => e.Id));
        Assert.Equal(70, page.Items[0].AvgWPM);
        Assert.Equal(0, page.Items[1].AvgWPM);
    }

    private Task<Guid> SeedPlayerAsync(string nick, params float[] raceWpms) =>
        app.ExecuteScopedAsync<AppDbContext, Guid>(async context =>
        {
            var user = new User(nick);
            var statistics = new PlayerStatistics(user.Id);
            foreach (var wpm in raceWpms)
                statistics.RegisterRace(new RaceParticipantResult(Guid.NewGuid(), user.Id, nick, wpm, 100, 0, 10, 1));

            context.Users.Add(user);
            context.Statistics.Add(statistics);
            await context.SaveChangesAsync();
            return user.Id;
        });
}
