using FasterNFaster.Api.UseCases.Interfaces.Users;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Hubs.Filters;

public class HubSuspensionFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var result = await next(invocationContext);

        if (HubUserId.TryGet(invocationContext.Context, out var userId))
        {
            // Resolved per-call (not constructor-injected) because IBanRepository is scoped
            // but this filter is a singleton, same as HubBanFilter does for its ban check.
            var bans = invocationContext.ServiceProvider.GetRequiredService<IBanRepository>();
            if (await bans.IsBannedAsync(userId))
                invocationContext.Context.Abort();
        }

        return result;
    }
}
