global using FluentValidation;
using DotNetEnv;
using FastEndpoints;
using FasterNFaster.Api.Infrastructure.Data;
using FasterNFaster.Api.Infrastructure.Hubs;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true)
    .CreateLogger();

if (File.Exists(".env"))
    Env.Load();

var builder = WebApplication.CreateBuilder(args);

var databaseUrl =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? throw new InvalidOperationException("DATABASE_URL is not set.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(databaseUrl));
builder.Services.AddSignalR();
builder.Services.AddFastEndpoints();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();

// // CORS
// var corsOrigins = (Environment.GetEnvironmentVariable("CORS_ORIGINS") ?? "").Split(
//     ',',
//     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
// );
// builder.Services.AddCors(options =>
// {
//     options.AddDefaultPolicy(policy =>
//     {
//         policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials(); // required for SignalR
//     });
// });

var app = builder.Build();

// app.UseCors();
app.UseOpenApi();
app.UseSwaggerUi();
app.UseFastEndpoints();
app.MapHub<GameHub>("/gameHub");

app.Run();
