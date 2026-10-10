using FasterNFaster.Api.Core.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Hubs.Filters;

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

            throw;
        }
    }
}
