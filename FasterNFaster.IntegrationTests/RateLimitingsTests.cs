using System.Net.Http.Json;
using FasterNFaster.Api.UseCases.Lobbies.Cleanup;
using FasterNFaster.Api.Web.Options.RateLimiting;
using FasterNFaster.IntegrationTests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class RateLimitingTests : IClassFixture<TestApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly TestApplicationFactory<Program> factory;
    private readonly RateLimitOptions rateLimitOptions;

    public Task InitializeAsync() => factory.ResetDb();
    public Task DisposeAsync() => Task.CompletedTask;
    public RateLimitingTests(TestApplicationFactory<Program> factory)
    {
        this.factory = factory;
        rateLimitOptions = factory.Services.GetRequiredService<IConfiguration>()
        .GetSection("RateLimiting")
        .Get<RateLimitOptions>()
        ?? throw new InvalidOperationException("RateLimiting section not found in configuration.");

    }

    [Fact]
    public async Task OneAuthStrictRequest_ShouldBeOk()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "10.99.0.2");

        var response1 = await AuthHelper.Register(client);

        Assert.Equal(System.Net.HttpStatusCode.Created, response1.StatusCode);
    }

    [Fact]
    public async Task ExceedAuthStrictLimit_ShouldReturn429()
    {
        var client = factory.CreateClient();
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
    public async Task ExceedAuthStrictLimitWaitWindow_ShouldBeFine()
    {
        TimeSpan testRateLimitWindow = TimeSpan.FromSeconds(3);
        var client = factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:AuthStrict:Window", testRateLimitWindow.ToString())).CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "10.99.0.3");

        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < rateLimitOptions.AuthStrict.PermitLimit; i++)
        {
            tasks.Add(AuthHelper.Register(client));
        }
        var okResponses = await Task.WhenAll(tasks);

        await Task.Delay(testRateLimitWindow);
        var nthRequest = rateLimitOptions.AuthStrict.PermitLimit;

        var nthResponse = await AuthHelper.Register(client);

        Assert.All(okResponses, (r) => Assert.Equal(System.Net.HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(System.Net.HttpStatusCode.Created, nthResponse.StatusCode);
    }
}