using Microsoft.AspNetCore.Hosting;

namespace FasterNFaster.IntegrationTests;

public class NoRateLimitApplicationFactory<TProgram>
    : TestApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimiting:Enabled", "false");
        base.ConfigureWebHost(builder);
    }
}
