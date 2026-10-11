using System.Net.Http.Json;
using FasterNFaster.Api.Presentation.Web.Auth.RegisterAnonymous;
using FasterNFaster.Api.Presentation.Web.Lobbies.CreateLobby;
using FasterNFaster.Api.Presentation.Web.Shared;
using Microsoft.Extensions.Configuration;

namespace FasterNFaster.IntegrationTests;

public class RateLimitingTests(TestApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<TestApplicationFactory<Program>>
{
    private readonly RateLimitOptions rateLimitOptions = fixture.Configuration
        .GetSection("RateLimiting")
        .Get<RateLimitOptions>()
        ?? throw new InvalidOperationException("RateLimiting section not found in configuration.");

    [Fact]
    public async Task ExceedAuthStrictLimit_ShouldReturn429()
    {
        var client = App.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "10.99.0.1");

        var tasks = new List<Task<HttpResponseMessage>>();
        for (var i = 0; i < rateLimitOptions.AuthStrict.PermitLimit; i++)
        {
            tasks.Add(AuthHelper.Register(client));
        }
        var okResponses = await Task.WhenAll(tasks);
        var nthRequest = rateLimitOptions.AuthStrict.PermitLimit;

        var nthResponse = await AuthHelper.Register(client);

        Assert.All(okResponses, (r) => Assert.Equal(System.Net.HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, nthResponse.StatusCode);
    }

    [Fact]
    public async Task ExceedAuthStrictLimitWaitWindow_ShouldBeOk()
    {
        var testRateLimitWindow = TimeSpan.FromSeconds(1);
        await using var shortWindowApp = Fixture.CreateApp(b => b.UseSetting("RateLimiting:AuthStrict:Window", testRateLimitWindow.ToString()));
        var client = shortWindowApp.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "10.99.0.3");

        var tasks = new List<Task<HttpResponseMessage>>();
        for (var i = 0; i < rateLimitOptions.AuthStrict.PermitLimit; i++)
        {
            tasks.Add(AuthHelper.Register(client));
        }
        var okResponses = await Task.WhenAll(tasks);

        await Task.Delay(testRateLimitWindow + TimeSpan.FromSeconds(1));
        var nthRequest = rateLimitOptions.AuthStrict.PermitLimit;

        var nthResponse = await AuthHelper.Register(client);

        Assert.All(okResponses, (r) => Assert.Equal(System.Net.HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(System.Net.HttpStatusCode.Created, nthResponse.StatusCode);
    }

    [Fact]
    public async Task ExceedLobbyCreateLimit_ShouldReturn429()
    {
        var client = App.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "10.99.0.4");
        var guestResponse = await client.PostAsJsonAsync(AuthHelper.GuestUri, new RegisterAnonymousRequest { Nick = "guest" });
        guestResponse.EnsureSuccessStatusCode();

        var tasks = new List<Task<HttpResponseMessage>>();
        for (var i = 0; i < rateLimitOptions.LobbyCreate.PermitLimit; i++)
        {
            tasks.Add(CreateLobby(client));
        }
        var okResponses = await Task.WhenAll(tasks);

        var nthResponse = await CreateLobby(client);

        Assert.All(okResponses, (r) => Assert.Equal(System.Net.HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, nthResponse.StatusCode);
    }

    private static Task<HttpResponseMessage> CreateLobby(HttpClient client) =>
        client.PostAsJsonAsync("/api/lobbies", new CreateLobbyRequest("test", false));
}
