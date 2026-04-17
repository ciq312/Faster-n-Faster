global using FluentValidation;
global using Serilog;
using DotNetEnv;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Infrastructure.Hubs;
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
using Microsoft.AspNetCore.Identity;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using FasterNFaster.Api.UseCases.Helpers.Implementations;

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
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddHostedService<RaceTickService>();
builder.Services.AddScoped<LobbyStateBroadcaster>();
builder.Services.AddScoped<IEventDispatcher, EventDispatcher>();
// builder
//     .Services.AddAuthentication("Token")
//     .AddCookie(
//         "Token",
//         options =>
//         {
//             options.Events.OnRedirectToLogin = context =>
//             {
//                 context.Response.StatusCode = 401;
//                 return Task.CompletedTask;
//             };
//         }
//     );

var rsa = RSA.Create();
rsa.ImportRSAPrivateKey(Convert.FromBase64String(builder.Configuration["JwtOptions:JWT_PRIVATE_TOKEN"]!), out _);
builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration.GetSection("JwtOptions:Issuer").Value,
        ValidAudience = builder.Configuration.GetSection("JwtOptions:Audience").Value,
        IssuerSigningKey = new RsaSecurityKey(rsa)
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
// app.UseMiddleware<TokenAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseOpenApi();
app.UseSwaggerUi();
app.UseFastEndpoints();
app.MapHub<GameHub>("/gameHub");

app.Run();
