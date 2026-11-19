using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace FasterNFaster.IntegrationTests;

public class HealthEndpointTests(TestApplicationFactory<Program> fixture) : IClassFixture<TestApplicationFactory<Program>>
{
    private const string Route = "/api/health";

    [Fact]
    public async Task BothDependenciesAvailable_ReturnsHealthy()
    {
        await using var app = fixture.CreateApp();
        using var client = CreateHealthClient(app);

        await ShouldReturnHealthAsync(client, HttpStatusCode.OK, "Healthy");
    }

    [Fact]
    public async Task PostgresPausedAfterWarmup_ReturnsUnhealthyUntilRecovery()
    {
        await using var app = fixture.CreateApp();
        using var client = CreateHealthClient(app);
        await ShouldReturnHealthAsync(client, HttpStatusCode.OK, "Healthy");

        await fixture.WithPostgresPausedAsync(() =>
            ShouldReturnHealthAsync(client, HttpStatusCode.ServiceUnavailable, "Unhealthy"));

        await ShouldRecoverAsync(client);
    }

    [Fact]
    public async Task RedisPausedAfterWarmup_ReturnsUnhealthyUntilRecovery()
    {
        await using var app = fixture.CreateApp();
        using var client = CreateHealthClient(app);
        await ShouldReturnHealthAsync(client, HttpStatusCode.OK, "Healthy");

        await fixture.WithRedisPausedAsync(() =>
            ShouldReturnHealthAsync(client, HttpStatusCode.ServiceUnavailable, "Unhealthy"));

        await ShouldRecoverAsync(client);
    }

    [Fact]
    public async Task RedisPausedBeforeFirstRequest_ReturnsUnhealthyUntilRecovery()
    {
        await using var app = fixture.CreateApp();
        using var client = CreateHealthClient(app);

        await fixture.WithRedisPausedAsync(() =>
            ShouldReturnHealthAsync(client, HttpStatusCode.ServiceUnavailable, "Unhealthy"));

        await ShouldRecoverAsync(client);
    }

    private static HttpClient CreateHealthClient(WebApplicationFactory<Program> app)
    {
        var client = app.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(12);
        return client;
    }

    private static async Task ShouldReturnHealthAsync(HttpClient client, HttpStatusCode statusCode, string status)
    {
        using var response = await client.GetAsync(Route);
        response.StatusCode.ShouldBe(statusCode);
        await ShouldHaveHealthBodyAsync(response, status);
    }

    private static async Task ShouldRecoverAsync(HttpClient client)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (true)
        {
            using var response = await client.GetAsync(Route, timeout.Token);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                await ShouldHaveHealthBodyAsync(response, "Healthy");
                return;
            }

            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            await Task.Delay(TimeSpan.FromMilliseconds(250), timeout.Token);
        }
    }

    private static async Task ShouldHaveHealthBodyAsync(HttpResponseMessage response, string status)
    {
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("text/plain");
        (await response.Content.ReadAsStringAsync()).ShouldBe(status);
    }
}
