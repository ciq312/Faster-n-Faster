using FasterNFaster.Api.Infrastructure.Users;
using FasterNFaster.Api.UseCases.Lobbies.FastReconnect;
using Microsoft.Extensions.Time.Testing;

namespace FasterNFaster.Tests.Handlers;

public class FastReconnectHandlerTests
{
    private readonly PendingRemovalRegistry registry = new();
    private readonly FakeTimeProvider time = new(DateTimeOffset.UtcNow);

    private Task StartHandler(Guid playerId) =>
        new FastReconnectHandler(registry, time)
            .Handle(new FastReconnectCommand(Guid.NewGuid(), playerId), CancellationToken.None);

    [Fact]
    public async Task GraceElapsed_CompletesAndRemovesPendingRemoval()
    {
        var playerId = Guid.NewGuid();
        var handler = StartHandler(playerId);

        time.Advance(FastReconnectHandler.ReconnectGracePeriod);
        await handler;

        Assert.False(registry.TryCancelPendingRemoval(playerId));
    }

    [Fact]
    public void GraceNotElapsed_StaysPending()
    {
        var handler = StartHandler(Guid.NewGuid());

        time.Advance(FastReconnectHandler.ReconnectGracePeriod - TimeSpan.FromSeconds(1));

        Assert.False(handler.IsCompleted);
    }

    [Fact]
    public async Task CancelledByRefresh_ThrowsOperationCanceled()
    {
        var playerId = Guid.NewGuid();
        var handler = StartHandler(playerId);

        registry.TryCancelPendingRemoval(playerId);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler);
    }

    [Fact]
    public async Task SecondDisconnect_CancelsFirstAndStaysCancellable()
    {
        var playerId = Guid.NewGuid();
        var first = StartHandler(playerId);
        var second = StartHandler(playerId);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.False(second.IsCompleted);
        Assert.True(registry.TryCancelPendingRemoval(playerId));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
    }
}
