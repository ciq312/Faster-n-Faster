using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Hubs.Filters;

public class HubExceptionFilter(ILogger<HubExceptionFilter> logger) : IHubFilter
{
    private readonly ILogger<HubExceptionFilter> logger = logger;

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{Method} failed on {ConnectionId}",
                invocationContext.HubMethodName, invocationContext.Context.ConnectionId);

            await invocationContext.Hub.Clients.Caller.SendAsync("Error", ex.Message);
            return null;
        }
    }
}
