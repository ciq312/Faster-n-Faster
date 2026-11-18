using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace FasterNFaster.Api.Presentation.Web.Health;

internal sealed class RedisHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    private readonly Lock connectionLock = new();
    private Task<IConnectionMultiplexer>? connection;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        var multiplexer = await GetConnectionAsync().WaitAsync(ct);
        await multiplexer.GetDatabase().PingAsync().WaitAsync(ct);
        return HealthCheckResult.Healthy();
    }

    private Task<IConnectionMultiplexer> GetConnectionAsync()
    {
        lock (connectionLock)
        {
            // The synchronous DI factory may outlive the request that started it.
            if (connection is null || connection.IsFaulted)
                connection = Task.Run(ResolveConnection);

            return connection;
        }
    }

    private IConnectionMultiplexer ResolveConnection()
    {
        using var scope = scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
    }
}
