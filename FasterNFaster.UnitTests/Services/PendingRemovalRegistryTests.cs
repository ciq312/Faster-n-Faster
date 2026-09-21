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
    public void TryCancel_AfterRemove_ReturnsFalseAndDoesNotCancel()
    {
        var registry = new PendingRemovalRegistry();
        var userId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        registry.StorePendingRemoval(userId, cts);

        registry.RemovePendingRemoval(userId);

        Assert.False(registry.TryCancelPendingRemoval(userId));
        Assert.False(cts.IsCancellationRequested);
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
