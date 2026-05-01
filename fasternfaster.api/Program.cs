global using FluentValidation;
global using Serilog;
using DotNetEnv;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Services;
using FasterNFaster.Api.Web.DependencyInversion;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using FasterNFaster.Api.Web.Hubs.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using FasterNFaster.Api.Web.Services;
using FasterNFaster.Api.Web.Services.Implementations;
using FasterNFaster.Api.UseCases.Factories.Interfaces;
using FasterNFaster.Api.UseCases.Factories.Implementations;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using FasterNFaster.Api.UseCases.Helpers.Implementations;
using Microsoft.AspNetCore.CookiePolicy;
using FasterNFaster.Api.Web.Options.JwtOptions;
using FasterNFaster.Api.Web.Options.AuthCookiesOptions;
using FasterNFaster.Api.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Google;
using FasterNFaster.Api.Web.Options.App;
using FasterNFaster.Api.Web.Options.Smtp;
using FasterNFaster.Api.Infrastructure.Smtp.EmailSender;
using FasterNFaster.Api.Infrastructure.Db.Tokens;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.Web.Hubs;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

if (File.Exists(".env"))
    Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<HubExceptionFilter>();
builder.Services.AddSignalR(options =>
{
    options.AddFilter<HubExceptionFilter>();
    options.AddFilter<HubSessionFilter>();
});
builder.Services.AddFastEndpoints();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();
builder.Services.AddHandlers();
builder.Services.AddDomainEventHandlers();
builder.Services.AddSingleton<ILobbyStore, LobbyStore>();
builder.Services.AddSingleton<IPassageProvider, RandomPassageProvider>();
builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
builder.Services.AddScoped<AppDbContext>();
builder.Services.AddScoped<IUserFactory, UserFactory>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IPasswordHelper, PasswordHelper>();
builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddSingleton<ISessionService, InMemorySessionService>();
builder.Services.AddSingleton<IRaceTickRegistry, RaceTickRegistry>();
builder.Services.AddSingleton<IPendingRemovalsRegistry, PendingRemovalRegistry>();
builder.Services.AddScoped<ITokenService, SlidingJwtTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IJwtTokenFactory, JwtTokenFactory>();
builder.Services.AddSingleton<ITokenStore, TokenStore>();
builder.Services.AddSingleton<ICookiesWriter, CookieWriter>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddHostedService<RaceTickService>();
builder.Services.AddScoped<LobbyStateBroadcaster>();
builder.Services.AddScoped<IEventDispatcher, EventDispatcher>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ITokenFactory, TokenFactory>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddScoped<IExternalLoginStore, ExternalLoginStore>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));
builder.Services.Configure<AuthCookiesOptions>(builder.Configuration.GetSection("AuthCookies"));
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("AppUrls"));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));


var rsa = RSA.Create();
rsa.ImportRSAPrivateKey(Convert.FromBase64String(builder.Configuration["JwtOptions:JWT_PRIVATE_TOKEN"]!), out _);
builder.Services.AddAuthentication(o =>
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
    options.ClientId = builder.Configuration["Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
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
        ValidIssuer = builder.Configuration.GetSection("JwtOptions:Issuer").Value,
        ValidAudience = builder.Configuration.GetSection("JwtOptions:Audience").Value,
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


builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://172.20.10.3:3000",
                "http://10.8.0.5:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var conString = builder.Configuration.GetConnectionString("DefaultConnection") ??
     throw new InvalidOperationException("Connection string 'DefaultConnectionString'" +
    " not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
      options.UseNpgsql(conString));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseOpenApi();
app.UseSwaggerUi();
app.UseFastEndpoints();
app.MapHub<GameHub>("/gameHub");

app.Run();

