using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Helpers;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.Infrastructure.Caching;
using FasterNFaster.Api.Infrastructure.Db.Statistics;
using FasterNFaster.Api.Infrastructure.Db.User;
using FasterNFaster.Api.Infrastructure.Db.Tokens;
using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.Infrastructure.Smtp.EmailSender;
using FasterNFaster.Api.Infrastructure.Users;
using FasterNFaster.Api.UseCases.Factories.Implementations;
using FasterNFaster.Api.UseCases.Factories.Interfaces;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Services;
using FasterNFaster.Api.UseCases.Services.Races;
using FasterNFaster.Api.UseCases.Services.Users;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using FasterNFaster.Api.Web.Realtime;
using FasterNFaster.Api.Web.Services.Interfaces;
using FasternFaster.Api.UseCases.Interfaces;
using Microsoft.AspNetCore.Identity;
using FasterNFaster.Api.Web.Services.Implementations;

namespace FasterNFaster.Api.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ILobbyStore, InMemoryLobbyStore>();
        services.AddSingleton<IPassageProvider, RandomPassageProvider>();

        services.AddSingleton<ICache, RedisCache>();

        services.AddScoped<IUserRepository, PostgresUserRepository>();
        services.AddScoped<PostgresStatisticsRepository>();
        services.AddScoped<IStatisticsRepository>(sp => new CachedStatisticsRepository(
            sp.GetRequiredService<PostgresStatisticsRepository>(), sp.GetRequiredService<ICache>()));
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IUserFactory, UserFactory>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IPasswordHelper, PasswordHelper>();
        services.AddScoped<BanRepository>();
        services.AddScoped<IBanRepository>(sp => new CachedBanRepository(
            sp.GetRequiredService<FakeBanRepository>(), sp.GetRequiredService<ICache>()));

        services.AddSingleton<ISessionService, InMemorySessionService>();
        services.AddSingleton<IRaceTickRegistry, RaceTickRegistry>();
        services.AddSingleton<IPendingRemovalsRegistry, PendingRemovalRegistry>();
        services.AddSingleton<IPlayerLocationRegistry, InMemoryPlayerLocationRegistry>();
        services.AddSingleton<IAggregateRootHelper, AggregateRootHelper>();
        services.AddSingleton<IEventDispatcher, MediatREventDispatcher>();
        services.AddSingleton<IAntiCheatPolicy, ConfiguredAntiCheatPolicy>();

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddHttpContextAccessor();
        services.AddSingleton<IJwtTokenFactory, JwtTokenFactory>();
        services.AddSingleton<IRefreshTokenRepository, RedisRefreshTokenRepository>();
        services.AddSingleton<IAuthTokenWriter, CookieWriter>();
        services.AddScoped<IConfirmTokenFactory, ConfirmTokenFactory>();
        services.AddSingleton<IConfirmTokenRepository, RedisConfirmTokenRepository>();
        services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();

        services.AddScoped<LeaderboardRepository>();
        services.AddScoped<ILeaderboardRepository>(sp => new CachedLeaderboardRepository(
            sp.GetRequiredService<LeaderboardRepository>(), sp.GetRequiredService<ICache>()));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<ILobbyStateBroadcaster, LobbyStateBroadcaster>();

        services.AddSingleton<IRaceBroadcaster, SignalRRaceBroadcaster>();
        services.AddSingleton<IBroadcaster, SignalRBroadcaster>();
        services.AddSingleton<RaceService>();
        services.AddSingleton<IRaceInternals>(sp => sp.GetRequiredService<RaceService>());
        services.AddSingleton<IRaceService>(sp => sp.GetRequiredService<RaceService>());

        services.AddSingleton<LobbyService>();
        services.AddSingleton<ILobbyInternals>(sp => sp.GetRequiredService<LobbyService>());
        services.AddSingleton<ILobbyService>(sp => sp.GetRequiredService<LobbyService>());

        services.AddSingleton<LobbyServiceFacade>();
        services.AddSingleton<IRaceTransitionService>(sp => sp.GetRequiredService<LobbyServiceFacade>());
        services.AddSingleton<ILobbyServiceFacade>(sp => sp.GetRequiredService<LobbyServiceFacade>());

        services.AddHostedService<RaceTickService>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServicesExtensions).Assembly);
            cfg.LicenseKey = config["MediatR:LicenseKey"];
        });

        return services;
    }
}
