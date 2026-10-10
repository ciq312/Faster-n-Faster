using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Api.Infrastructure.Users;

public class PendingRemovalRegistry : IPendingRemovalsRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> pendingRemovals = new();

    public bool CompletePendingRemoval(Guid userId, CancellationTokenSource cts) =>
        pendingRemovals.TryRemove(KeyValuePair.Create(userId, cts));

    public void StorePendingRemoval(Guid userId, CancellationTokenSource cts)
    {
        // Not AddOrUpdate: its factory may run several times, so it can't tell which CTS was actually replaced and must be released.
        while (true)
        {
            if (pendingRemovals.TryGetValue(userId, out var previous))
            {
                if (pendingRemovals.TryUpdate(userId, cts, previous))
                {
                    Release(previous);
                    return;
                }
            }
            else if (pendingRemovals.TryAdd(userId, cts))
            {
                return;
            }
        }
    }

    public bool TryCancelPendingRemoval(Guid userId)
    {
        if (!pendingRemovals.TryRemove(userId, out var cts)) return false;

        Release(cts);
        return true;
    }

    private static void Release(CancellationTokenSource cts)
    {
        cts.Cancel();
        cts.Dispose();
    }
}
