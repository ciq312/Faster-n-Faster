using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.Web.Hubs.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace FasterNFaster.Api.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var redisConn = config.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string not found.");

        var dbConn = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));

        services.AddSingleton<HubExceptionFilter>();
        services.AddSingleton<HubCheatFilter>();
        services.AddSingleton<HubBanFilter>();
        services.AddSignalR(options =>
        {
            options.AddFilter<HubExceptionFilter>();
            options.AddFilter<HubCheatFilter>();
            options.AddFilter<HubBanFilter>();
        })
        .AddStackExchangeRedis(redisConn);

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dbConn));

        return services;
    }
}
