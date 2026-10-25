using System.Net;
using System.Net.Http.Json;
using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.Presentation.Web.Users.GetUserProfile;
using FasterNFaster.Api.UseCases.Users.Ports;
using Shouldly;

namespace FasterNFaster.IntegrationTests;

public class UserProfileTests(NoRateLimitApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    [Fact]
    public async Task ProfileWithStatistics_ReturnsFlatStatistics()
    {
        var client = App.CreateClient();
        var player = await AuthHelper.RegisterAndLoginAsync(App, client, AuthHelper.NewPlayer());
        await App.ExecuteScopedAsync<IStatisticsRepository>(async repo =>
        {
            var statistics = new PlayerStatistics(player.UserId);
            statistics.RegisterRace(new RaceParticipantResult(
                Guid.NewGuid(), player.UserId, player.Nick, wpm: 80, accuracy: 95, mistakeCount: 0, wordsTyped: 40,
                finishPosition: 1, finishedAt: TimeProvider.System.GetUtcNow()));
            repo.Add(statistics);
            await repo.SaveAsync();
        });

        var response = await client.GetAsync("api/users/me/profile");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserProfileResponse>();
        body.ShouldNotBeNull();
        body.Nick.ShouldBe(player.Nick);
        body.RacesTyped.ShouldBe(1);
        body.Wins.ShouldBe(1);
    }

    [Fact]
    public async Task ProfileWithoutStatistics_Returns404()
    {
        var client = App.CreateClient();
        await AuthHelper.RegisterAndLoginAsync(App, client, AuthHelper.NewPlayer());

        var response = await client.GetAsync("api/users/me/profile");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.ShouldNotBeNull();
        body.Reason.ShouldBe("No statistics for this player");
    }
}
