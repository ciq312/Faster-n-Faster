using FasterNFaster.Api.Infrastructure.Shared.Persistence;
using FasterNFaster.Api.Presentation.Web.Realtime;
using FasterNFaster.Api.Presentation.Web.Realtime.Filters;
using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FasterNFaster.Api.Presentation.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(sp.GetRequiredService<IOptions<ConnectionStrings>>().Value.Redis!));

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

        services.AddDbContext<AppDbContext>((sp, options) => options.UseNpgsql(sp.GetRequiredService<IOptions<ConnectionStrings>>().Value.DefaultConnection!));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        return services;
    }
}
