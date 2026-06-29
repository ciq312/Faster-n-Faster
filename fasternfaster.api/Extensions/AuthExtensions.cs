using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace FasterNFaster.Api.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration config)
    {
        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(config["JwtOptions:JWT_PRIVATE_TOKEN"]!), out _);

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
                ValidIssuer = config["JwtOptions:Issuer"],
                ValidAudience = config["JwtOptions:Audience"],
                IssuerSigningKey = new RsaSecurityKey(rsa),
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies["access_token"];
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        return services;
    }
}
