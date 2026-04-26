using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Api.UseCases.Services;

public class PendingRemovalRegistry : IPendingRemovalsRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> pendingRemovals = new();

    public Task StorePendingRemoval(Guid userId, CancellationTokenSource cts)
    {
        pendingRemovals[userId] = cts;
        return Task.CompletedTask;
    }

    public Task<bool> TryGetPendingRemoval(Guid userId, out CancellationTokenSource cts)
    {
        cts = pendingRemovals[userId];
        if (cts == null) return Task.FromResult(false);
        return Task.FromResult(true);
    }
}