using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Presentation.Web.Realtime;

public class SignalRBroadcaster(
    IHubContext<GameHub, IGameClient> hub,
    ISessionService sessionService,
    ILobbyStore lobbies) : IBroadcaster
{
    public async Task Broadcast(IAudience audience, Func<IGameClient, Task> send)
    {
        var clients = await Resolve(audience);
        if (clients is null) return;

        await send(clients);
    }

    private async Task<IGameClient?> Resolve(IAudience audience) => audience switch
    {
        LobbyAudience a => Connections(await MembersOf(a.LobbyId)),
        LobbyExceptAudience a => Connections((await MembersOf(a.LobbyId)).Where(id => id != a.UserId)),
        PlayerAudience a => ConnectionOf(a.UserId),
        PlayersAudience a => Connections(a.PlayerIds),
        _ => throw new ArgumentOutOfRangeException(nameof(audience))
    };

    private async Task<IReadOnlyList<Guid>> MembersOf(Guid lobbyId) =>
        await lobbies.TryRead(lobbyId, l => l.GetPlayerIds()) ?? [];

    private IGameClient Connections(IEnumerable<Guid> userIds) =>
        hub.Clients.Clients(userIds.Select(sessionService.GetActiveSession).OfType<string>().ToList());

    private IGameClient? ConnectionOf(Guid userId)
    {
        var connectionId = sessionService.GetActiveSession(userId);
        return connectionId is null ? null : hub.Clients.Client(connectionId);
    }
}
