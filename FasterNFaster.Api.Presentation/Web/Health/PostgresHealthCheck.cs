using FasterNFaster.Api.Infrastructure.Shared.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FasterNFaster.Api.Presentation.Web.Health;

internal sealed class PostgresHealthCheck(IServiceScopeFactory scopeFactory, TimeSpan timeout) : IHealthCheck
{
    private readonly Lock probeLock = new();
    private Task<bool>? probe;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        var canConnect = await GetProbeAsync().WaitAsync(ct);
        return canConnect ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
    }

    private Task<bool> GetProbeAsync()
    {
        lock (probeLock)
        {
            // A paused server may delay cancellation, so let concurrent checks wait on one probe.
            if (probe is null || probe.IsCompleted)
                probe = ProbeAsync();

            return probe;
        }
    }

    private async Task<bool> ProbeAsync()
    {
        using var cancellation = new CancellationTokenSource(timeout);
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Database.CanConnectAsync(cancellation.Token);
    }
}
