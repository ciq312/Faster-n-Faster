global using FluentValidation;
global using Serilog;
using DotNetEnv;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Infrastructure.Hubs;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Services;
using FasterNFaster.Api.Web.DependencyInversion;
using FasterNFaster.Api.Web.Middleware;
using FasterNFaster.Api.Infrastructure;

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
builder.Services.AddSingleton<ILobbyStore, LobbyStore>();
builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddScoped<LobbyStateBroadcaster>();
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
            .WithOrigins("http://localhost:3000")
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
