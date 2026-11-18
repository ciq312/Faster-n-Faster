using FasterNFaster.Api.UseCases.AntiCheat.Ports;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Presentation.Web.Realtime.Filters;

public class HubBanFilter : IHubFilter
{
    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        if (HubUserId.TryGet(context.Context, out var userId))
        {
            var banService = context.ServiceProvider.GetRequiredService<IBanRepository>();
            if (await banService.IsBannedAsync(userId))
            {
                var hub = context.ServiceProvider.GetRequiredService<IHubContext<GameHub, IGameClient>>();
                await hub.Clients.Client(context.Context.ConnectionId).Banned("You are banned");
                context.Context.Abort();
                return;
            }
        }

        await next(context);
    }
}
