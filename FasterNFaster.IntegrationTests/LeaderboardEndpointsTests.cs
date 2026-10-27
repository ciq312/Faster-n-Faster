using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Presentation.Web.Auth.RegisterAnonymous;
using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.UseCases.AntiCheat.Ports;
using FasterNFaster.Api.UseCases.Leaderboards.GetLeaderboard;
using Shouldly;
using static FasterNFaster.IntegrationTests.StatisticsSeeder;

namespace FasterNFaster.IntegrationTests;

public class LeaderboardEndpointsTests(NoRateLimitApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    private const string MyRankUri = "api/leaderboards/me";

    [Fact]
    public async Task MyRank_LoggedOut_Returns401NotAuthenticated()
    {
        var response = await App.CreateClient().GetAsync(MyRankUri);

        await ShouldBeErrorAsync(response, HttpStatusCode.Unauthorized, ErrorCodes.NotAuthenticated);
    }

    [Fact]
    public async Task MyRank_Guest_Returns403Forbidden()
    {
        var client = App.CreateClient();
        var guestResponse = await client.PostAsJsonAsync(AuthHelper.GuestUri, new RegisterAnonymousRequest { Nick = "guest" });
        guestResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync(MyRankUri);

        await ShouldBeErrorAsync(response, HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task MyRank_PlayerWithoutStatistics_Returns404NotOnLeaderboard()
    {
        var client = App.CreateClient();
        await AuthHelper.RegisterAndLoginAsync(App, client, AuthHelper.NewPlayer());

        var response = await client.GetAsync(MyRankUri);

        await ShouldBeErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.NotOnLeaderboard);
    }

    [Fact]
    public async Task MyRank_BannedPlayer_Returns404NotOnLeaderboard()
    {
        var client = App.CreateClient();
        var player = await AuthHelper.RegisterAndLoginAsync(App, client, AuthHelper.NewPlayer());
        await App.SeedStatisticsAsync(player.UserId, Race(80));
        await App.ExecuteScopedAsync<IBanRepository>(repo => repo.BanAsync(player.UserId, "cheating"));

        var response = await client.GetAsync(MyRankUri);

        await ShouldBeErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.NotOnLeaderboard);
    }

    [Fact]
    public async Task MyRank_RankedPlayer_ReturnsRowMatchingListPosition()
    {
        var client = App.CreateClient();
        var player = await AuthHelper.RegisterAndLoginAsync(App, client, AuthHelper.NewPlayer());
        await App.SeedStatisticsAsync(player.UserId, Race(50));
        await SeedOtherPlayersAsync(wpm: 90, count: 2);
        await SeedOtherPlayersAsync(wpm: 20, count: 2);

        var response = await client.GetAsync($"{MyRankUri}?sort=BestWpm&descending=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mine = await response.Content.ReadFromJsonAsync<LeaderboardResultDTO>();
        var list = await client.GetFromJsonAsync<GetLeaderboardResults>("api/leaderboards?sort=BestWpm&descending=true&pageSize=100");
        mine.ShouldNotBeNull();
        list.ShouldNotBeNull();
        mine.Rank.ShouldBe(3);
        list.Items.ShouldContain(row => row.Id == mine.Id && row.Rank == mine.Rank);
        mine.Nick.ShouldBe(player.Nick);
        mine.BestWPM.ShouldBe(50);
    }

    [Fact]
    public async Task MyRank_AscendingWins_RanksByRequestedOrder()
    {
        var client = App.CreateClient();
        var player = await AuthHelper.RegisterAndLoginAsync(App, client, AuthHelper.NewPlayer());
        await App.SeedStatisticsAsync(player.UserId, Race(50, finishPosition: 1));
        await App.SeedPlayerAsync("winnerA", Race(90, finishPosition: 1), Race(90, finishPosition: 1));
        await App.SeedPlayerAsync("winnerB", Race(20, finishPosition: 1), Race(20, finishPosition: 1));
        const string order = "sort=Wins&descending=false";

        var mine = await client.GetFromJsonAsync<LeaderboardResultDTO>($"{MyRankUri}?{order}");
        var list = await client.GetFromJsonAsync<GetLeaderboardResults>($"api/leaderboards?{order}&pageSize=100");

        mine.ShouldNotBeNull().Rank.ShouldBe(1);
        list.ShouldNotBeNull().Items.ShouldContain(row => row.Id == mine.Id && row.Rank == mine.Rank);
    }

    [Fact]
    public async Task MyRank_StatsChangeBetweenCalls_ReturnsFreshRank()
    {
        var client = App.CreateClient();
        var player = await AuthHelper.RegisterAndLoginAsync(App, client, AuthHelper.NewPlayer());
        await App.SeedStatisticsAsync(player.UserId, Race(50));
        var before = await client.GetFromJsonAsync<LeaderboardResultDTO>(MyRankUri);

        await SeedOtherPlayersAsync(wpm: 90, count: 1);
        var after = await client.GetFromJsonAsync<LeaderboardResultDTO>(MyRankUri);

        before.ShouldNotBeNull().Rank.ShouldBe(1);
        after.ShouldNotBeNull().Rank.ShouldBe(2);
    }

    [Fact]
    public async Task List_ItemsCarryNick()
    {
        await SeedOtherPlayersAsync(wpm: 70, count: 1);

        var response = await App.CreateClient().GetAsync("api/leaderboards");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = body.RootElement.GetProperty("items")[0];
        item.TryGetProperty("nick", out _).ShouldBeTrue();
        item.TryGetProperty("playerName", out _).ShouldBeFalse();
    }

    private static async Task ShouldBeErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        response.StatusCode.ShouldBe(status);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.ShouldNotBeNull().Code.ShouldBe(code);
    }

    private async Task SeedOtherPlayersAsync(float wpm, int count)
    {
        for (var i = 0; i < count; i++)
            await App.SeedPlayerAsync($"other{Guid.NewGuid():N}"[..12], Race(wpm));
    }
}
