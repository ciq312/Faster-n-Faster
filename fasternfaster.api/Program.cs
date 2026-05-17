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
using Microsoft.AspNetCore.HttpOverrides;
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
using FasterNFaster.Api.UseCases.Services.Auth;
using StackExchange.Redis;
using FasterNFaster.Api.Core.Helpers;
using FasterNFaster.Api.UseCases.Services.Races;
using FasternFaster.Api.UseCases.Interfaces;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("log.txt")
    .CreateLogger();

if (File.Exists(".env"))
    Env.Load();

var builder = WebApplication.CreateBuilder(args);

var redisConn = builder.Configuration.GetConnectionString("Redis") ?? throw new NullReferenceException("Redis conn string not found");

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));

builder.Services.AddSingleton<HubExceptionFilter>();
builder.Services.AddSignalR(options =>
{
    options.AddFilter<HubExceptionFilter>();
    options.AddFilter<HubSessionFilter>();
})
.AddStackExchangeRedis(redisConn);
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
builder.Services.AddScoped<IBanService, BanService>();
builder.Services.AddSingleton<ISessionService, InMemorySessionService>();
builder.Services.AddSingleton<IRaceTickRegistry, RaceTickRegistry>();
builder.Services.AddSingleton<IPendingRemovalsRegistry, PendingRemovalRegistry>();
builder.Services.AddScoped<ITokenService, SlidingJwtTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IJwtTokenFactory, JwtTokenFactory>();
builder.Services.AddSingleton<ITokenStore, RedisTokenStore>();
builder.Services.AddSingleton<ICookiesWriter, CookieWriter>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddHostedService<RaceTickService>();
builder.Services.AddScoped<LobbyStateBroadcaster>();
builder.Services.AddSingleton<IEventDispatcher, EventDispatcher>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ITokenFactory, TokenFactory>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddScoped<IExternalLoginStore, ExternalLoginStore>();
builder.Services.AddSingleton<IAggregateRootHelper, AggregateRootHelper>();
builder.Services.AddSingleton<ILobbySessionService, LobbySessionService>();
builder.Services.AddSingleton<RaceService>();
builder.Services.AddSingleton<IRaceCoordinator>(sp => sp.GetRequiredService<RaceService>());
builder.Services.AddSingleton<IRaceService>(sp => sp.GetRequiredService<RaceService>());
builder.Services.AddSingleton<LobbyService>();
builder.Services.AddSingleton<ILobbyCoordinator>(sp => sp.GetRequiredService<LobbyService>());
builder.Services.AddSingleton<ILobbyService>(sp => sp.GetRequiredService<LobbyService>());
builder.Services.AddSingleton<LobbySessionService>();
builder.Services.AddSingleton<IRaceTransitionService>(sp => sp.GetRequiredService<LobbySessionService>());
builder.Services.AddSingleton<ILobbySessionService>(sp => sp.GetRequiredService<LobbySessionService>());

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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? throw new InvalidOperationException("Cors:AllowedOrigins not configured");

builder.Services.AddCors(options =>
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

var conString = builder.Configuration.GetConnectionString("DefaultConnection") ??
     throw new InvalidOperationException("Connection string 'DefaultConnectionString'" +
    " not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
      options.UseNpgsql(conString));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { },
    KnownProxies = { }
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseOpenApi();
app.UseSwaggerUi();
app.UseFastEndpoints();
app.MapHub<GameHub>("/gameHub");

app.Run();

