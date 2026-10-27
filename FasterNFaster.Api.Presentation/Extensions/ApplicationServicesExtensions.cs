using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Interfaces;
using FasterNFaster.Api.Domain.Interfaces.Events;
using FasterNFaster.Api.Infrastructure.AntiCheat;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.Infrastructure.Leaderboards;
using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.Infrastructure.Shared.Caching;
using FasterNFaster.Api.Infrastructure.Users;
using FasterNFaster.Api.Presentation.Web.AntiCheat;
using FasterNFaster.Api.Presentation.Web.Auth;
using FasterNFaster.Api.Presentation.Web.Realtime;
using FasterNFaster.Api.UseCases;
using FasterNFaster.Api.UseCases.AntiCheat.Ports;
using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Auth.Services;
using FasterNFaster.Api.UseCases.Leaderboards.Ports;
using FasterNFaster.Api.UseCases.Lobbies.Cleanup;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Lobbies.Services;
using FasterNFaster.Api.UseCases.Races.Ports;
using FasterNFaster.Api.UseCases.Races.RefreshPassage;
using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Shared;
using FasterNFaster.Api.UseCases.Users.Ports;
using FasterNFaster.Api.UseCases.Users.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FasterNFaster.Api.Presentation.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IPassageProvider, RandomPassageProvider>();

        services.AddSingleton<RedisCache>();
        services.AddSingleton<ICache>(sp => new CoalescingCache(sp.GetRequiredService<RedisCache>()));

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
        services.AddSingleton<IRaceStore, InMemoryRaceStore>();
        services.AddSingleton<PassageRefresher>();
        services.AddSingleton<RaceStateConflator>();
        services.AddSingleton<IRaceResultQueue, RaceResultQueue>();
        services.AddHostedService<RaceResultProcessor>();

        services.AddSingleton<InMemoryLobbyStore>();
        services.AddSingleton<ILobbyStore>(sp => new TrackingLobbyStore(
            sp.GetRequiredService<InMemoryLobbyStore>(), sp.GetRequiredService<ILobbyStateTracker>()));
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
