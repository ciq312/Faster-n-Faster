using System.Net;
using System.Net.Http.Json;
using FasterNFaster.Api.Presentation.Web.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace FasterNFaster.IntegrationTests;

public abstract class IntegrationTestBase(TestApplicationFactory<Program> fixture) : IAsyncLifetime
{
    protected TestApplicationFactory<Program> Fixture { get; } = fixture;
    protected WebApplicationFactory<Program> App { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        await Fixture.ResetAsync();
        App = Fixture.CreateApp(ConfigureApp);
    }

    public async Task DisposeAsync() => await App.DisposeAsync();

    protected virtual void ConfigureApp(IWebHostBuilder builder)
    {
    }

    protected static async Task ShouldBeErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        response.StatusCode.ShouldBe(status);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.ShouldNotBeNull().Code.ShouldBe(code);
    }
}
