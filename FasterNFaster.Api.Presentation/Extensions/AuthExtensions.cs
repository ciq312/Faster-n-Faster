using System.Security.Cryptography;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FasterNFaster.Api.Presentation.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration config)
    {
        var accessTokenCookieName = config.GetSection("AuthCookies").Get<AuthCookiesOptions>()?.AccessTokenCookieName
            ?? new AuthCookiesOptions().AccessTokenCookieName;

        var jwt = config.GetSection("JwtOptions").Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrEmpty(jwt.PrivateKey))
            throw new InvalidOperationException("JwtOptions:PrivateKey is not configured.");

        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(jwt.PrivateKey), out _);

        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddCookie("External", options =>
        {
            // Short-lived cookie that holds Google claims between challenge and callback only.
            options.Cookie.Name = "External";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        })
        .AddGoogle("Google", options =>
        {
            options.ClientId = config["Google:ClientId"]!;
            options.ClientSecret = config["Google:ClientSecret"]!;
            options.SignInScheme = "External";
            options.Scope.Add("openid");
            options.Scope.Add("email");
            options.Scope.Add("profile");
            options.CallbackPath = "/api/auth/google/signin";
        })
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RoleClaimType = "role",
                NameClaimType = "name",
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new RsaSecurityKey(rsa),
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies[accessTokenCookieName];
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        return services;
    }
}
