using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Api.Infrastructure.Users;

public class PendingRemovalRegistry : IPendingRemovalsRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> pendingRemovals = new();

    public void RemovePendingRemoval(Guid userId)
    {
        pendingRemovals.Remove(userId, out _);
    }

    public void StorePendingRemoval(Guid userId, CancellationTokenSource cts)
    {
        pendingRemovals[userId] = cts;
    }

    public bool TryCancelPendingRemoval(Guid userId)
    {
        var cts = pendingRemovals.GetValueOrDefault(userId);
        if (cts == null) return false;
        cts.Cancel();
        return true;
    }
}
