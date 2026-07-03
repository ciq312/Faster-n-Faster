using System.Threading.RateLimiting;
using FasterNFaster.Api.Web.Options.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace FasterNFaster.Api.Extensions;

public static class RateLimitPolicies
{
    public const string AuthStrict = "auth-strict";
    public const string AuthModerate = "auth-moderate";
    public const string Lookup = "lookup";
}

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection("RateLimiting").Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = OnRejected;

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => IpPartition(context, options.Global, options.Enabled));

            limiter.AddPolicy(RateLimitPolicies.AuthStrict,
                context => IpPartition(context, options.AuthStrict, options.Enabled));
            limiter.AddPolicy(RateLimitPolicies.AuthModerate,
                context => IpPartition(context, options.AuthModerate, options.Enabled));
            limiter.AddPolicy(RateLimitPolicies.Lookup,
                context => IpPartition(context, options.Lookup, options.Enabled));
        });

        return services;
    }

    private static RateLimitPartition<string> IpPartition(
        HttpContext context, RateLimitOptions.WindowLimit limit, bool enabled)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!enabled)
            return RateLimitPartition.GetNoLimiter(clientIp);

        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit.PermitLimit,
            Window = limit.Window,
            QueueLimit = 0
        });
    }

    private static async ValueTask OnRejected(OnRejectedContext context, CancellationToken ct)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many requests, try again shortly." }, ct);
    }
}
