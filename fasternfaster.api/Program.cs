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
using FasterNFaster.Api.Web.Middleware;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Web.Lobbies.LobbyState;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true)
    .CreateLogger();

if (File.Exists(".env"))
    Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddFastEndpoints();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();
builder.Services.AddHandlers();
builder.Services.AddDomainEventHandlers();
builder.Services.AddSingleton<ILobbyStore, LobbyStore>();
builder.Services.AddSingleton<IPassageProvider, RandomPassageProvider>();
builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
builder.Services.AddScoped<AppDbContext>();
builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddSingleton<IRaceTickRegistry, RaceTickRegistry>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddHostedService<RaceTickService>();
builder.Services.AddScoped<LobbyStateBroadcaster>();
builder.Services.AddScoped<IEventDispatcher, EventDispatcher>();
builder
    .Services.AddAuthentication("Token")
    .AddCookie(
        "Token",
        options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            };
        }
    );
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
app.UseMiddleware<TokenAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseOpenApi();
app.UseSwaggerUi();
app.UseFastEndpoints();
app.MapHub<GameHub>("/gameHub");

app.Run();
