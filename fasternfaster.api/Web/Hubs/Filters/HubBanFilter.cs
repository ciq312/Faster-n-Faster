using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.Hubs.Filters;

public class HubBanFilter : IHubFilter
{
    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        if (TryGetUserId(context.Context, out var userId))
        {
            var banService = context.ServiceProvider.GetRequiredService<IBanRepository>();
            if (await banService.IsBannedAsync(userId))
            {
                await context.Hub.Clients.Caller.SendAsync(Methods.Banned, "You are banned");
                context.Context.Abort();
                return;
            }
        }

        await next(context);
    }

    private static bool TryGetUserId(HubCallerContext context, out Guid userId)
    {
        userId = default;
        var sub = context.User?.FindFirst("sub")?.Value;
        return sub is not null && Guid.TryParse(sub, out userId);
    }
}
