using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Hubs.Filters;

public class HubBanFilter : IHubFilter
{
    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        if (HubUserId.TryGet(context.Context, out var userId))
        {
            var banService = context.ServiceProvider.GetRequiredService<IBanRepository>();
            if (await banService.IsBannedAsync(userId))
            {
                await context.Hub.Clients.Caller.SendAsync(GameEvents.Banned, "You are banned");
                context.Context.Abort();
                return;
            }
        }

        await next(context);
    }
}
