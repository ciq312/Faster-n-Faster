global using FluentValidation;
global using Serilog;
using DotNetEnv;
using FastEndpoints;
using FasterNFaster.Api.Extensions;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.Infrastructure.Db;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

ThreadPool.SetMinThreads(200, 200);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("log.txt")
    .CreateLogger();

if (File.Exists(".env"))
    Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddWebServices(builder.Configuration);
builder.Services.AddApiRateLimiting(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseExceptionHandler();

// The API is only reachable through Caddy (its port isn't published in prod),
// so the immediate hop is trusted to set X-Forwarded-For. Without clearing the
// defaults the middleware ignores the header and every user shares Caddy's IP.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}
app.UseFastEndpoints();
app.MapHub<GameHub>("/gameHub");

app.Run();
