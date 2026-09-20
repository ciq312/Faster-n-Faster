using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;

namespace FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;

public class LobbyStateScope(
    ILobbyStateTracker tracker,
    ILobbyAccess lobbies,
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

            await broadcaster.Broadcast(Audience.Lobby(lobbyId), GameEvents.LobbyState, await lobbyQuery.GetLobbyState(lobbyId));
        }
    }
}
