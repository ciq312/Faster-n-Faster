using DotNetEnv;
using FastEndpoints;
using FasterNFaster.Api.Presentation.Extensions;
using FasterNFaster.Api.Presentation.Web.Realtime;
using FasterNFaster.Api.Presentation.Web.Shared;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateLogger();

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddWebServices(builder.Configuration);
builder.Services.AddApiRateLimiting(builder.Configuration);

var app = builder.Build();

// Outside UseExceptionHandler, or business errors thrown as exceptions would log as 500s.
app.UseSerilogRequestLogging();
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
app.UseFastEndpoints(c =>
{
    c.Errors.ProducesMetadataType = typeof(ApiErrorResponse);
    c.Errors.ResponseBuilder = (failures, ctx, statusCode) =>
    {
        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        return new ApiErrorResponse()
        {
            Reason = "One or more validation errors occurred.",
            Code = "validation_failed",
            Errors = errors
        };
    };
});
app.MapHub<GameHub>("/gameHub");

app.Run();
