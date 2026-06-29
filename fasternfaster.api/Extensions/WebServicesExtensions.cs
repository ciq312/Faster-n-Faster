using FastEndpoints;
using FasterNFaster.Api.Web.Exceptions;
using FasterNFaster.Api.Web.Options.AntiCheat;
using FasterNFaster.Api.Web.Options.App;
using FasterNFaster.Api.Web.Options.AuthCookiesOptions;
using FasterNFaster.Api.Web.Options.Smtp;
using FasterNFaster.Api.UseCases.Users.ResendVerification;
using FasterNFaster.Api.UseCases.Users.RequestPasswordReset;
using FasterNFaster.Api.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Extensions;

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
        services.Configure<ResendVerificationOptions>(config.GetSection("ResendVerification"));
        services.Configure<RequestPasswordResetOptions>(config.GetSection("RequestPasswordReset"));

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ResendVerificationOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<RequestPasswordResetOptions>>().Value);
        services.Configure<ResetPasswordOptions>(config.GetSection("ResetPassword"));
        services.Configure<VerifyEmailOptions>(config.GetSection("VerifyEmail"));

        return services;
    }
}
