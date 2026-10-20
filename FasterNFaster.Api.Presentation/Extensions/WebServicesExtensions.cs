using FastEndpoints;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Api.Infrastructure.Shared;
using FasterNFaster.Api.Presentation.Web.AntiCheat;
using FasterNFaster.Api.Presentation.Web.Auth;
using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.UseCases.AntiCheat.Sanctions;
using FasterNFaster.Api.UseCases.Auth.Services;

namespace FasterNFaster.Api.Presentation.Extensions;

public static class WebServicesExtensions
{
    public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddFastEndpoints();
        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocument();
        services.AddExceptionHandler<StatusExceptionHandler>();
        services.AddProblemDetails();

        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? throw new InvalidOperationException("Cors:AllowedOrigins not configured.");

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.Configure<JwtOptions>(config.GetSection("JwtOptions"));
        services.Configure<AuthCookiesOptions>(config.GetSection("AuthCookies"));
        services.Configure<AppOptions>(config.GetSection("AppUrls"));
        services.Configure<SmtpOptions>(config.GetSection("Smtp"));
        services.Configure<AntiCheatOptions>(config.GetSection("AntiCheat"));
        services.Configure<AntiCheatSanctionOptions>(config.GetSection("AntiCheatSanctions"));
        services.Configure<ConfirmTokenOptions>(config.GetSection("ConfirmTokens"));
        services.Configure<LobbyCleanupOptions>(config.GetSection("LobbyCleanup"));

        return services;
    }
}
