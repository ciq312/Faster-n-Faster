using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Realtime;

public class SignalRBroadcaster(
    IHubContext<GameHub> hub,
    ISessionService sessionService,
    ILobbyStore lobbyStore) : IBroadcaster
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
        LobbyAudience a => Connections(MembersOf(a.LobbyId)),
        LobbyExceptAudience a => Connections(MembersOf(a.LobbyId).Where(id => id != a.UserId)),
        PlayerAudience a => ConnectionOf(a.UserId),
        _ => throw new ArgumentOutOfRangeException(nameof(audience))
    };


    private IEnumerable<Guid> MembersOf(Guid lobbyId) =>
        lobbyStore.Get(lobbyId)?.Players.Select(p => p.User.Id) ?? [];

    private IClientProxy Connections(IEnumerable<Guid> userIds) =>
        hub.Clients.Clients(userIds.Select(sessionService.GetActiveSession).OfType<string>().ToList());

    private IClientProxy? ConnectionOf(Guid userId)
    {
        var connectionId = sessionService.GetActiveSession(userId);
        return connectionId is null ? null : hub.Clients.Client(connectionId);
    }
}
