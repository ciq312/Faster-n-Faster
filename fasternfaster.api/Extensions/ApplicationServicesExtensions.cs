using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Helpers;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Infrastructure.Db.Tokens;
using FasterNFaster.Api.Infrastructure.Smtp.EmailSender;
using FasterNFaster.Api.UseCases.Factories.Implementations;
using FasterNFaster.Api.UseCases.Factories.Interfaces;
using FasterNFaster.Api.UseCases.Helpers.Implementations;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Services;
using FasterNFaster.Api.UseCases.Services.Auth;
using FasterNFaster.Api.UseCases.Services.Races;
using FasterNFaster.Api.UseCases.Services.Users;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using FasterNFaster.Api.Web.Services;
using FasterNFaster.Api.Web.Services.Implementations;
using FasterNFaster.Api.Web.Services.Interfaces;
using FasternFaster.Api.UseCases.Interfaces;
using Microsoft.AspNetCore.Identity;
using FasterNFaster.Api.Infrastructure;

namespace FasterNFaster.Api.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ILobbyStore, LobbyStore>();
        services.AddSingleton<IPassageProvider, RandomPassageProvider>();

        services.AddScoped<IUserRepository, PostgresUserRepository>();
        services.AddScoped<IStatisticsRepository, PostgresStatisticsRepository>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IUserFactory, UserFactory>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IPasswordHelper, PasswordHelper>();
        services.AddScoped<IBanService, BanService>();

        services.AddSingleton<ISessionService, InMemorySessionService>();
        services.AddSingleton<IRaceTickRegistry, RaceTickRegistry>();
        services.AddSingleton<IPendingRemovalsRegistry, PendingRemovalRegistry>();
        services.AddSingleton<IPlayerLocationRegistry, InMemoryPlayerLocationRegistry>();
        services.AddSingleton<IAggregateRootHelper, AggregateRootHelper>();
        services.AddSingleton<IEventDispatcher, MediatREventDispatcher>();
        services.AddSingleton<IAntiCheatPolicy, ConfiguredAntiCheatPolicy>();

        services.AddScoped<ITokenService, SlidingJwtTokenService>();
        services.AddHttpContextAccessor();
        services.AddSingleton<IJwtTokenFactory, JwtTokenFactory>();
        services.AddSingleton<ITokenStore, RedisTokenStore>();
        services.AddSingleton<ICookiesWriter, CookieWriter>();
        services.AddScoped<ITokenFactory, TokenFactory>();
        services.AddScoped<ITokenRepository, TokenRepository>();
        services.AddScoped<IExternalLoginStore, ExternalLoginStore>();

        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<ILobbyStateBroadcaster, LobbyStateBroadcaster>();

        services.AddSingleton<IRaceBroadcaster, SignalRRaceBroadcaster>();
        services.AddSingleton<RaceService>();
        services.AddSingleton<IRaceCoordinator>(sp => sp.GetRequiredService<RaceService>());
        services.AddSingleton<IRaceService>(sp => sp.GetRequiredService<RaceService>());

        services.AddSingleton<LobbyService>();
        services.AddSingleton<ILobbyInternals>(sp => sp.GetRequiredService<LobbyService>());
        services.AddSingleton<ILobbyService>(sp => sp.GetRequiredService<LobbyService>());

        services.AddSingleton<LobbySessionService>();
        services.AddSingleton<IRaceTransitionService>(sp => sp.GetRequiredService<LobbySessionService>());
        services.AddSingleton<ILobbySessionService>(sp => sp.GetRequiredService<LobbySessionService>());

        services.AddHostedService<RaceTickService>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServicesExtensions).Assembly);
            cfg.LicenseKey = config["MediatR:LicenseKey"];
        });

        return services;
    }
}
