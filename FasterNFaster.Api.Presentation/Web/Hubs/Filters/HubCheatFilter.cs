using FasterNFaster.Api.Core.Exceptions.Races;
using FasterNFaster.Api.UseCases.Lobbies.BanForCheat;
using FasterNFaster.Api.UseCases.Realtime;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Hubs.Filters;

public class HubCheatFilter(ILogger<HubCheatFilter> logger) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (CheaterDetectedException ex)
        {
            await HandleCheat(invocationContext, ex);
            return null;
        }
    }

    private async Task HandleCheat(HubInvocationContext ctx, CheaterDetectedException ex)
    {
        var sub = ctx.Context.User?.FindFirst("sub")?.Value;
        if (sub is null || !Guid.TryParse(sub, out var userId)) return;

        logger.LogWarning("Cheating detected for user {UserId}: {Reason}", userId, ex.Reason);

        var sender = ctx.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(new BanForCheatCommand(userId, ex.Reason));
        await ctx.Hub.Clients.Caller.SendAsync(GameEvents.Banned, $"Cheating detected: {ex.Reason}");
        ctx.Context.Abort();
    }
}
