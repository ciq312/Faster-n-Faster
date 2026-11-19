using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Infrastructure.Shared.Telemetry;
using Microsoft.AspNetCore.Diagnostics;

namespace FasterNFaster.Api.Presentation.Web.Shared;

public class StatusExceptionHandler(ILogger<StatusExceptionHandler> logger) : IExceptionHandler
{
    private const string FallbackReason = "An unexpected error occurred";
    private const string FallbackCode = "error";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Since .NET 10 the exception handler middleware stays silent once a handler returns true, so unexpected errors are logged here.
        var statusCode = MapStatusCode(exception);
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception in {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            var endpoint = httpContext.Features.Get<IExceptionHandlerFeature>()?.Endpoint ?? httpContext.GetEndpoint();
            var routeTemplate = (endpoint as RouteEndpoint)?.RoutePattern.RawText;
            logger.LogError("{TelemetryOperation} ({ErrorKind}) on {RouteTemplate}: {ErrorCode} ({StatusCode})",
                SafeTelemetryLog.HttpError, SafeTelemetryLog.Classify(exception), routeTemplate, FallbackCode, statusCode);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(ToResponse(exception), cancellationToken);
        return true;
    }

    private static ApiErrorResponse ToResponse(Exception exception) =>
        exception is StatusException ex
            ? new ApiErrorResponse
            {
                Reason = ex.Message,
                Code = ex.Code ?? FallbackCode,
                Errors = ex.Extensions?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            }
            : new ApiErrorResponse { Reason = FallbackReason, Code = FallbackCode };

    private static int MapStatusCode(Exception exception) =>
        exception switch
        {
            ForbiddenException => StatusCodes.Status403Forbidden,
            BadRequestException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError,
        };
}
