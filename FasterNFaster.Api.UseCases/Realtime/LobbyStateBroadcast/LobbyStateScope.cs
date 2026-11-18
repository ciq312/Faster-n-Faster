using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Lobbies.Services;
using FasterNFaster.Api.UseCases.Realtime.Ports;

namespace FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;

public class LobbyStateScope(
    ILobbyStateTracker tracker,
    ILobbyStore lobbies,
    ILobbyQuery lobbyQuery,
    IBroadcaster broadcaster) : ILobbyStateScope
{
    public async Task<T> Run<T>(Func<Task<T>> operation)
    {
        if (!tracker.TryBeginScope()) return await operation();

        T result;
        IReadOnlyCollection<Guid> changed;

        try
        {
            result = await operation();
        }
        finally
        {
            changed = tracker.EndScope();
        }

        await Broadcast(changed);

        return result;
    }

    public Task Run(Func<Task> operation) =>
        Run<object?>(async () =>
        {
            await operation();
            return null;
        });

    private async Task Broadcast(IReadOnlyCollection<Guid> lobbyIds)
    {
        foreach (var lobbyId in lobbyIds)
        {
            if (!lobbies.Exists(lobbyId)) continue;

            var state = await lobbyQuery.GetLobbyState(lobbyId);
            await broadcaster.Broadcast(Audience.Lobby(lobbyId), c => c.LobbyState(state));
        }
    }
}
