using System.Diagnostics;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Infrastructure.Shared.Telemetry;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Presentation.Web.Realtime.Filters;

public class HubExceptionFilter(ILogger<HubExceptionFilter> logger) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (StatusException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (Exception ex) when (ex is not HubException)
        {
            Guid? userId = HubUserId.TryGet(invocationContext.Context, out var id) ? id : null;
            logger.LogError(ex, "Unhandled exception in hub method {HubMethod} for user {UserId}", invocationContext.HubMethodName, userId);
            logger.LogError("{TelemetryOperation} ({ErrorKind}) in hub method {HubMethod} for user {UserId} {ErrorType} {StackTrace} {TraceId}",
                SafeTelemetryLog.SignalRHubError,
                SafeTelemetryLog.Classify(ex),
                invocationContext.HubMethodName,
                userId,
                ex.GetType().Name,
                SafeTelemetryLog.GetSafeStackTrace(ex),
                Activity.Current?.TraceId.ToString());

            throw;
        }
    }
}
