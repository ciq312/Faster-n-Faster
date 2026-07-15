using FasterNFaster.IntegrationTests;
using Microsoft.AspNetCore.Hosting;

public class NoRateLimitApplicationFactory<TProgram>
    : TestApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimiting:Enabled", "false");
        base.ConfigureWebHost(builder);
    }
}