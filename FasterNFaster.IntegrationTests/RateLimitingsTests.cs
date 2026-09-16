using System.Net.Http.Json;
using FasterNFaster.Api.UseCases.Lobbies.Cleanup;
using FasterNFaster.Api.Web.Options.RateLimiting;
using FasterNFaster.IntegrationTests;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

public class RateLimitingTests : IClassFixture<TestApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly TestApplicationFactory<Program> fixture;
    private readonly RateLimitOptions rateLimitOptions;
    private WebApplicationFactory<Program> app = null!;

    public RateLimitingTests(TestApplicationFactory<Program> fixture)
    {
        this.fixture = fixture;
        rateLimitOptions = fixture.Configuration
        .GetSection("RateLimiting")
        .Get<RateLimitOptions>()
        ?? throw new InvalidOperationException("RateLimiting section not found in configuration.");

    }

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        app = fixture.CreateApp();
    }

    public async Task DisposeAsync() => await app.DisposeAsync();

    [Fact]
    public async Task OneAuthStrictRequest_ShouldBeOk()
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "10.99.0.2");

        var response1 = await AuthHelper.Register(client);

        Assert.Equal(System.Net.HttpStatusCode.Created, response1.StatusCode);
    }

    [Fact]
    public async Task ExceedAuthStrictLimit_ShouldReturn429()
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "10.99.0.1");

        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < rateLimitOptions.AuthStrict.PermitLimit; i++)
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
        TimeSpan testRateLimitWindow = TimeSpan.FromSeconds(1);
        await using var shortWindowApp = fixture.CreateApp(b => b.UseSetting("RateLimiting:AuthStrict:Window", testRateLimitWindow.ToString()));
        var client = shortWindowApp.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "10.99.0.3");

        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < rateLimitOptions.AuthStrict.PermitLimit; i++)
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
}