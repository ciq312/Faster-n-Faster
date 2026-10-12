using FasterNFaster.Api.Infrastructure.Shared.Persistence;
using FasterNFaster.Api.Presentation.Web.Realtime;
using FasterNFaster.Api.Presentation.Web.Realtime.Filters;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace FasterNFaster.Api.Presentation.Extensions;

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
        services.AddSingleton<SignalRConnectionTerminator>();
        services.AddSingleton<IConnectionTerminator>(sp => sp.GetRequiredService<SignalRConnectionTerminator>());
        services.AddSingleton<HubBanFilter>();
        services.AddSignalR(options =>
        {
            options.AddFilter<HubExceptionFilter>();
            // Must come before HubBanFilter so no sanction falls between the connect-time check and tracking.
            options.AddFilter<SignalRConnectionTerminator>();
            options.AddFilter<HubBanFilter>();
        });

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dbConn));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        return services;
    }
}
