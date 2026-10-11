using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Interfaces;
using FasterNFaster.Api.Domain.Interfaces.Events;
using FasterNFaster.Api.Infrastructure.AntiCheat;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.Infrastructure.Db.Statistics;
using FasterNFaster.Api.Infrastructure.Db.Users;
using FasterNFaster.Api.Infrastructure.Helpers;
using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.Infrastructure.Shared.Caching;
using FasterNFaster.Api.Infrastructure.Users;
using FasterNFaster.Api.Presentation.Web.AntiCheat;
using FasterNFaster.Api.Presentation.Web.Realtime;
using FasterNFaster.Api.Presentation.Web.Services.Implementations;
using FasterNFaster.Api.Presentation.Web.Services.Interfaces;
using FasterNFaster.Api.UseCases;
using FasterNFaster.Api.UseCases.AntiCheat.Ports;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Lobbies.Cleanup;
using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Services.Lobbies;
using FasterNFaster.Api.UseCases.Services.Races;
using FasterNFaster.Api.UseCases.Services.Users;
using FasterNFaster.Api.UseCases.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FasterNFaster.Api.Presentation.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ILobbyRepository, InMemoryLobbyRepository>();
        services.AddSingleton<IPassageProvider, RandomPassageProvider>();

        services.AddSingleton<ICache, RedisCache>();

        services.AddScoped<IUserRepository, PostgresUserRepository>();
        services.AddScoped<IStatisticsRepository, PostgresStatisticsRepository>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IPasswordHelper, PasswordHelper>();
        services.AddScoped<IBanRepository, BanRepository>();

        services.AddSingleton<ISessionService, InMemorySessionService>();
        services.AddSingleton<IRaceTickRegistry, RaceTickRegistry>();
        services.AddSingleton<IPendingRemovalsRegistry, PendingRemovalRegistry>();
        services.AddSingleton<IStrikeRegistry, StrikeRegistry>();
        services.AddSingleton<IPlayerLocationRegistry, InMemoryPlayerLocationRegistry>();
        services.AddSingleton<IEventDispatcher, MediatREventDispatcher>();
        services.AddSingleton<IAntiCheatPolicy, ConfiguredAntiCheatPolicy>();

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddHttpContextAccessor();
        services.AddSingleton<IJwtTokenFactory, JwtTokenFactory>();
        services.AddSingleton<IRefreshTokenRepository, RedisRefreshTokenRepository>();
        services.AddSingleton<IAuthTokenWriter, CookieWriter>();
        services.AddScoped<IConfirmTokenFactory, ConfirmTokenFactory>();
        services.AddSingleton<IConfirmTokenRepository, RedisConfirmTokenRepository>();
        services.AddScoped<IConfirmTokenIssuer, ConfirmTokenIssuer>();
        services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();

        services.AddScoped<LeaderboardRepository>();
        services.AddScoped<ILeaderboardRepository>(sp => new CachedLeaderboardRepository(
            sp.GetRequiredService<LeaderboardRepository>(), sp.GetRequiredService<ICache>()));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddSingleton<IBroadcaster, SignalRBroadcaster>();
        services.AddSingleton<IRaceAccess, RaceAccess>();
        services.AddSingleton<RaceStateConflator>();
        services.AddSingleton<IRaceResultQueue, RaceResultQueue>();
        services.AddHostedService<RaceResultProcessor>();

        services.AddSingleton<LobbyAccess>();
        services.AddSingleton<ILobbyAccess>(sp => sp.GetRequiredService<LobbyAccess>());
        services.AddSingleton<EmptyLobbyRemover>();

        services.AddSingleton<ILobbyQuery, LobbyQuery>();

        services.AddSingleton<ILobbyStateTracker, LobbyStateTracker>();
        services.AddSingleton<ILobbyStateScope, LobbyStateScope>();

        services.AddHostedService<RaceTickService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<EmptyLobbySweepService>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(UseCasesAssemblyClass).Assembly);
            cfg.AddOpenBehavior(typeof(LobbyStateFlushBehavior<,>));
            cfg.LicenseKey = config["MediatR:LicenseKey"];
        });

        return services;
    }
}
