using FasterNFaster.Api.Core.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Hubs.Filters;

public class HubExceptionFilter() : IHubFilter
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
    }
}
