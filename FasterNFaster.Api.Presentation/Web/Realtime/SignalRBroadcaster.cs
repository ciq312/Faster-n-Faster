using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Realtime;

public class SignalRBroadcaster(
    IHubContext<GameHub> hub,
    ISessionService sessionService,
    ILobbyAccess lobbies) : IBroadcaster
{
    public async Task Broadcast<T>(IAudience audience, string eventName, T payload)
    {
        var clients = await Resolve(audience);
        if (clients is null) return;

        await clients.SendAsync(eventName, payload);
    }

    public async Task Broadcast(IAudience audience, string eventName)
    {
        var clients = await Resolve(audience);
        if (clients is null) return;

        await clients.SendAsync(eventName);
    }

    private async Task<IClientProxy?> Resolve(IAudience audience) => audience switch
    {
        LobbyAudience a => Connections(await MembersOf(a.LobbyId)),
        LobbyExceptAudience a => Connections((await MembersOf(a.LobbyId)).Where(id => id != a.UserId)),
        PlayerAudience a => ConnectionOf(a.UserId),
        PlayersAudience a => Connections(a.PlayerIds),
        _ => throw new ArgumentOutOfRangeException(nameof(audience))
    };

    private async Task<IReadOnlyList<Guid>> MembersOf(Guid lobbyId) =>
        await lobbies.TryRead(lobbyId, l => l.GetPlayerIds()) ?? [];

    private IClientProxy Connections(IEnumerable<Guid> userIds) =>
        hub.Clients.Clients(userIds.Select(sessionService.GetActiveSession).OfType<string>().ToList());

    private IClientProxy? ConnectionOf(Guid userId)
    {
        var connectionId = sessionService.GetActiveSession(userId);
        return connectionId is null ? null : hub.Clients.Client(connectionId);
    }
}
