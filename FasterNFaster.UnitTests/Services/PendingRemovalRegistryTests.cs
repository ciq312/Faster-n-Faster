using FasterNFaster.Api.Infrastructure.Users;

namespace FasterNFaster.Tests.Services;

public class PendingRemovalRegistryTests
{
    [Fact]
    public void TryCancel_NothingStored_ReturnsFalse()
    {
        var registry = new PendingRemovalRegistry();

        Assert.False(registry.TryCancelPendingRemoval(Guid.NewGuid()));
    }

    [Fact]
    public void TryCancel_Stored_CancelsAndReturnsTrue()
    {
        var registry = new PendingRemovalRegistry();
        var userId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        registry.StorePendingRemoval(userId, cts);

        Assert.True(registry.TryCancelPendingRemoval(userId));
        Assert.True(cts.IsCancellationRequested);
    }

    [Fact]
    public void TryCancel_Stored_RemovesEntry()
    {
        var registry = new PendingRemovalRegistry();
        var userId = Guid.NewGuid();
        registry.StorePendingRemoval(userId, new CancellationTokenSource());

        registry.TryCancelPendingRemoval(userId);

        Assert.False(registry.TryCancelPendingRemoval(userId));
    }

    [Fact]
    public void TryCancel_Stored_DisposesCts()
    {
        var registry = new PendingRemovalRegistry();
        var userId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        registry.StorePendingRemoval(userId, cts);

        registry.TryCancelPendingRemoval(userId);

        Assert.Throws<ObjectDisposedException>(() => cts.Token);
    }

    [Fact]
    public void Complete_CurrentCts_RemovesEntryWithoutCancelling()
    {
        var registry = new PendingRemovalRegistry();
        var userId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        registry.StorePendingRemoval(userId, cts);

        registry.CompletePendingRemoval(userId, cts);

        Assert.False(registry.TryCancelPendingRemoval(userId));
        Assert.False(cts.IsCancellationRequested);
    }

    [Fact]
    public void Complete_CurrentCts_ReturnsTrue()
    {
        var registry = new PendingRemovalRegistry();
        var userId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        registry.StorePendingRemoval(userId, cts);

        Assert.True(registry.CompletePendingRemoval(userId, cts));
    }

    [Fact]
    public void Complete_StaleCts_ReturnsFalseAndKeepsNewerPendingRemoval()
    {
        var registry = new PendingRemovalRegistry();
        var userId = Guid.NewGuid();
        using var stale = new CancellationTokenSource();
        using var current = new CancellationTokenSource();
        registry.StorePendingRemoval(userId, stale);
        registry.StorePendingRemoval(userId, current);

        Assert.False(registry.CompletePendingRemoval(userId, stale));
        Assert.True(registry.TryCancelPendingRemoval(userId));
        Assert.True(current.IsCancellationRequested);
    }

    [Fact]
    public void Store_OverExistingEntry_CancelsPreviousAndKeepsNewCancellable()
    {
        var registry = new PendingRemovalRegistry();
        var userId = Guid.NewGuid();
        using var previous = new CancellationTokenSource();
        using var current = new CancellationTokenSource();
        registry.StorePendingRemoval(userId, previous);

        registry.StorePendingRemoval(userId, current);

        Assert.True(previous.IsCancellationRequested);
        Assert.False(current.IsCancellationRequested);
        Assert.True(registry.TryCancelPendingRemoval(userId));
        Assert.True(current.IsCancellationRequested);
    }

    [Fact]
    public void Store_OverExistingEntry_DisposesPrevious()
    {
        var registry = new PendingRemovalRegistry();
        var userId = Guid.NewGuid();
        var previous = new CancellationTokenSource();
        using var current = new CancellationTokenSource();
        registry.StorePendingRemoval(userId, previous);

        registry.StorePendingRemoval(userId, current);

        Assert.Throws<ObjectDisposedException>(() => previous.Token);
    }

    [Fact]
    public async Task ConcurrentStoreCompleteAndCancel_DoesNotThrowAndLeavesNoEntry()
    {
        const int rounds = 50;
        const int actions = 20;

        for (var round = 0; round < rounds; round++)
        {
            var registry = new PendingRemovalRegistry();
            var userId = Guid.NewGuid();

            var exception = await Record.ExceptionAsync(() => ConcurrentRunner.RunTogether(
                Enumerable.Range(0, actions).Select<int, Func<Task>>(i => i % 2 == 0
                    ? () => Task.Run(() => SimulateHandler(registry, userId))
                    : () => Task.Run(() => registry.TryCancelPendingRemoval(userId)))));

            Assert.Null(exception);
            Assert.False(registry.TryCancelPendingRemoval(userId));
        }
    }

    private static void SimulateHandler(PendingRemovalRegistry registry, Guid userId)
    {
        var cts = new CancellationTokenSource();
        registry.StorePendingRemoval(userId, cts);
        if (registry.CompletePendingRemoval(userId, cts))
            cts.Dispose();
    }

    [Fact]
    public void TryCancel_OtherUsersRemovalUnaffected()
    {
        var registry = new PendingRemovalRegistry();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        using var ctsA = new CancellationTokenSource();
        using var ctsB = new CancellationTokenSource();
        registry.StorePendingRemoval(userA, ctsA);
        registry.StorePendingRemoval(userB, ctsB);

        registry.TryCancelPendingRemoval(userA);

        Assert.False(ctsB.IsCancellationRequested);
    }
}
