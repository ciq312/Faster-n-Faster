using FastEndpoints;
using FastEndpoints.Swagger;
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
        services.SwaggerDocument(o =>
        {
            o.ShortSchemaNames = true;
            o.AutoTagPathSegmentIndex = 2;
        });
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

        services.Configure<AuthCookiesOptions>(config.GetSection("AuthCookies"));
        services.Configure<AppOptions>(config.GetSection("AppUrls"));
        services.Configure<SmtpOptions>(config.GetSection("Smtp"));
        services.Configure<AntiCheatOptions>(config.GetSection("AntiCheat"));
        services.Configure<AntiCheatSanctionOptions>(config.GetSection("AntiCheatSanctions"));
        services.Configure<ConfirmTokenOptions>(config.GetSection("ConfirmTokens"));
        services.Configure<LobbyCleanupOptions>(config.GetSection("LobbyCleanup"));
        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection("JwtOptions"))
            .Validate(opt => !string.IsNullOrEmpty(opt.PrivateKey), "JwtOptions:PrivateKey is not configured.")
            .ValidateOnStart();
        services.AddOptions<ConnectionStrings>()
            .Bind(config.GetSection("ConnectionStrings"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.DefaultConnection) && !string.IsNullOrWhiteSpace(o.Redis),
                "ConnectionStrings:DefaultConnection and ConnectionStrings:Redis must be configured.")
            .ValidateOnStart();

        return services;
    }
}
