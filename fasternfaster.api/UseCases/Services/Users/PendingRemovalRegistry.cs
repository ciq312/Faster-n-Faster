using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Api.UseCases.Services;

public class PendingRemovalRegistry : IPendingRemovalsRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> pendingRemovals = new();

    public Task RemovePendingRemoval(Guid userId)
    {
        pendingRemovals.Remove(userId, out _);
        return Task.CompletedTask;
    }

    public Task StorePendingRemoval(Guid userId, CancellationTokenSource cts)
    {
        pendingRemovals[userId] = cts;
        return Task.CompletedTask;
    }

    public Task<bool> TryCancelPendingRemoval(Guid userId)
    {
        var cts = pendingRemovals.GetValueOrDefault(userId);
        if (cts == null) return Task.FromResult(false);
        cts.Cancel();
        return Task.FromResult(true);
    }


}