using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using FasterNFaster.Api.UseCases.Services;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Handlers;

public class LobbyStateScopeTests
{
    private static (LobbyStateScope Scope, FakeBroadcaster Broadcaster, LobbyStateTracker Tracker) Build(LobbyTestContext context)
    {
        var broadcaster = new FakeBroadcaster();
        var scope = new LobbyStateScope(context.Tracker, context.LobbyAccess, context.LobbyQuery, broadcaster);

        return (scope, broadcaster, context.Tracker);
    }

    [Fact]
    public async Task Run_BroadcastsOnce_WhenLobbyMarkedRepeatedly()
    {
        var context = await LobbyFactory.WithPlayers(new User("test"));
        var (scope, broadcaster, tracker) = Build(context);

        await scope.Run(() =>
        {
            tracker.MarkChanged(context.LobbyId);
            tracker.MarkChanged(context.LobbyId);
            tracker.MarkChanged(context.LobbyId);
            return Task.CompletedTask;
        });

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(GameEvents.LobbyState, sent.EventName);
        var audience = Assert.IsType<LobbyAudience>(sent.Audience);
        Assert.Equal(context.LobbyId, audience.LobbyId);
    }

    [Fact]
    public async Task Run_BroadcastsNothing_WhenNothingChanged()
    {
        var context = await LobbyFactory.WithPlayers(new User("test"));
        var (scope, broadcaster, _) = Build(context);

        await scope.Run(() => Task.CompletedTask);

        Assert.Empty(broadcaster.Broadcasts);
    }

    [Fact]
    public async Task Run_FlushesOnceAtOuterScope_WhenNested()
    {
        var context = await LobbyFactory.WithPlayers(new User("test"));
        var (scope, broadcaster, tracker) = Build(context);

        await scope.Run(async () =>
        {
            tracker.MarkChanged(context.LobbyId);

            await scope.Run(() =>
            {
                tracker.MarkChanged(context.LobbyId);
                return Task.CompletedTask;
            });

            Assert.Empty(broadcaster.Broadcasts);
        });

        Assert.Single(broadcaster.Broadcasts);
    }

    [Fact]
    public async Task Run_SkipsLobby_RemovedDuringOperation()
    {
        var context = await LobbyFactory.WithPlayers(new User("test"));
        var (scope, broadcaster, tracker) = Build(context);

        await scope.Run(async () =>
        {
            tracker.MarkChanged(context.LobbyId);
            await context.LobbyAccess.Remove(context.LobbyId);
        });

        Assert.Empty(broadcaster.Broadcasts);
    }

    [Fact]
    public async Task MarkChanged_OutsideScope_IsIgnored()
    {
        var context = await LobbyFactory.WithPlayers(new User("test"));
        var (scope, broadcaster, tracker) = Build(context);

        tracker.MarkChanged(context.LobbyId);

        await scope.Run(() => Task.CompletedTask);

        Assert.Empty(broadcaster.Broadcasts);
    }

    [Fact]
    public async Task Run_ReturnsHandlerResult()
    {
        var context = await LobbyFactory.WithPlayers(new User("test"));
        var (scope, _, tracker) = Build(context);

        var result = await scope.Run(() =>
        {
            tracker.MarkChanged(context.LobbyId);
            return Task.FromResult(42);
        });

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Run_DoesNotBroadcast_WhenOperationThrows()
    {
        var context = await LobbyFactory.WithPlayers(new User("test"));
        var (scope, broadcaster, tracker) = Build(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.Run(() =>
        {
            tracker.MarkChanged(context.LobbyId);
            throw new InvalidOperationException("boom");
        }));

        Assert.Empty(broadcaster.Broadcasts);

        await scope.Run(() => Task.CompletedTask);

        Assert.Empty(broadcaster.Broadcasts);
    }
}
