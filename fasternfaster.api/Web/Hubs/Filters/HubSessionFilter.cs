using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Hubs.Filters;

public class HubSessionFilter(ISessionService sessionService) : IHubFilter
{
    private readonly ISessionService sessionService = sessionService;

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var userIdClaim = invocationContext.Context.User?.FindFirst("UserId")?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            throw new HubException("User ID claim is missing or invalid.");

        var callerConnectionId = invocationContext.Context.ConnectionId;
        await ValidateSession(userId, callerConnectionId, invocationContext.Hub.Clients.Client(callerConnectionId));

        return await next(invocationContext);
    }

    private async Task ValidateSession(Guid userId, string callerConnectionId, IClientProxy previousSession)
    {
        var currentConnectionId = sessionService.GetActiveSession(userId);

        if (currentConnectionId is not null && currentConnectionId != callerConnectionId)
            await previousSession.SendAsync("AnotherSessionStarted");

    }
}