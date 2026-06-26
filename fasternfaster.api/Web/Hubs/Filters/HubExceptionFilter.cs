using FasterNFaster.Api.Core.Exceptions;
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
        catch (StatusException ex)
        {
            throw new HubException(ex.Message);
        }
    }
}
