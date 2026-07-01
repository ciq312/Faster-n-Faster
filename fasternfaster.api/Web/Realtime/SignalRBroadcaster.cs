using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.Realtime;

public class SignalRBroadcaster(IHubContext<GameHub> hub, ISessionService sessionService) : IBroadcaster
{
    public Task Broadcast<T>(IAudience audience, string eventName, T payload)
    {
        var clients = Resolve(audience);
        return clients is null ? Task.CompletedTask : clients.SendAsync(eventName, payload);
    }

    public Task Broadcast(IAudience audience, string eventName)
    {
        var clients = Resolve(audience);
        return clients is null ? Task.CompletedTask : clients.SendAsync(eventName);
    }

    private IClientProxy? Resolve(IAudience audience) => audience switch
    {
        LobbyAudience a => hub.Clients.Group(LobbyGroup(a.LobbyId)),
        PlayerAudience a => ResolvePlayer(a.UserId),
        LobbyExceptAudience a => ResolveLobbyExcept(a.LobbyId, a.UserId),
        _ => throw new ArgumentOutOfRangeException(nameof(audience))
    };

    private IClientProxy? ResolvePlayer(Guid userId)
    {
        var connectionId = sessionService.GetActiveSession(userId);
        return connectionId is null ? null : hub.Clients.Client(connectionId);
    }

    private IClientProxy ResolveLobbyExcept(Guid lobbyId, Guid userId)
    {
        var connectionId = sessionService.GetActiveSession(userId);
        return connectionId is null
            ? hub.Clients.Group(LobbyGroup(lobbyId))
            : hub.Clients.GroupExcept(LobbyGroup(lobbyId), connectionId);
    }
}
