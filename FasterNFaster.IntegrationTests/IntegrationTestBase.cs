using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

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
}
