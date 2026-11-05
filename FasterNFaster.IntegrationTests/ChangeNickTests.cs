using System.Net;
using System.Net.Http.Json;
using FasterNFaster.Api.Presentation.Web.Auth.Me;
using FasterNFaster.Api.Presentation.Web.Users.ChangeNick;
using Shouldly;

namespace FasterNFaster.IntegrationTests;

public class ChangeNickTests(NoRateLimitApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    private const string ChangeNickUri = "api/users/me/nick";

    [Fact]
    public async Task ValidNick_ReturnsOk_AndNewTokensCarryIt()
    {
        var client = App.CreateClient();
        await AuthHelper.RegisterAndLoginAsync(App, client, AuthHelper.NewPlayer());

        var response = await client.PutAsJsonAsync(ChangeNickUri, new ChangeNickRequest("Renamed"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var me = await client.GetFromJsonAsync<MeResponse>("api/auth/me");
        me.ShouldNotBeNull();
        me.UserName.ShouldBe("Renamed");
    }

    [Theory]
    [InlineData("")]
    [InlineData("ElevenChars")]
    public async Task InvalidNick_Returns400(string nick)
    {
        var client = App.CreateClient();
        await AuthHelper.RegisterAndLoginAsync(App, client, AuthHelper.NewPlayer());

        var response = await client.PutAsJsonAsync(ChangeNickUri, new ChangeNickRequest(nick));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Guest_Returns403()
    {
        var client = App.CreateClient();
        await AuthHelper.RegisterGuestAsync(client);

        var response = await client.PutAsJsonAsync(ChangeNickUri, new ChangeNickRequest("Renamed"));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NotLoggedIn_Returns401()
    {
        var client = App.CreateClient();

        var response = await client.PutAsJsonAsync(ChangeNickUri, new ChangeNickRequest("Renamed"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
