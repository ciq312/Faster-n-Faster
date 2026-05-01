using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Hubs.Filters;

public class HubSessionFilter(ISessionService sessionService, ILobbyStore lobbyStore) : IHubFilter
{
    private readonly ISessionService sessionService = sessionService;
    private readonly ILobbyStore lobbyStore = lobbyStore;

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var userIdClaim = invocationContext.Context.User?.FindFirst("sub")?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            throw new HubException("Not authorized");

        var callerConnectionId = invocationContext.Context.ConnectionId;

        return await next(invocationContext);
    }


}