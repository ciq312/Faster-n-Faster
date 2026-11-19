using FasterNFaster.Api.Presentation.Web.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FasterNFaster.Api.Presentation.Extensions;

internal static class HealthCheckExtensions
{
    public static IServiceCollection AddDependencyHealthChecks(this IServiceCollection services)
    {
        var timeout = TimeSpan.FromSeconds(3);

        services.AddSingleton(sp => new PostgresHealthCheck(sp.GetRequiredService<IServiceScopeFactory>(), timeout));
        services.AddSingleton<RedisHealthCheck>();
        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", failureStatus: HealthStatus.Unhealthy, timeout: timeout)
            .AddCheck<RedisHealthCheck>("redis", failureStatus: HealthStatus.Unhealthy, timeout: timeout);

        return services;
    }
}
