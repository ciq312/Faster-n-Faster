using FasterNFaster.Api.Presentation.Web.Realtime;
using FasterNFaster.UnitTests.Fakes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace FasterNFaster.UnitTests.Realtime;

public class SignalRConnectionTerminatorTests
{
    private readonly SignalRConnectionTerminator terminator = new();

    [Fact]
    public async Task UserWithTwoConnections_AbortsBoth()
    {
        var userId = Guid.NewGuid();
        var first = await Connect(userId);
        var second = await Connect(userId);

        terminator.Terminate(userId);

        Assert.True(first.Aborted);
        Assert.True(second.Aborted);
    }

    [Fact]
    public async Task OtherUsersConnection_IsNotAborted()
    {
        var other = await Connect(Guid.NewGuid());

        terminator.Terminate(Guid.NewGuid());

        Assert.False(other.Aborted);
    }

    [Fact]
    public async Task DisconnectedConnection_IsNotAborted()
    {
        var userId = Guid.NewGuid();
        var context = await Connect(userId);

        await terminator.OnDisconnectedAsync(Lifetime(context), null, (_, _) => Task.CompletedTask);
        terminator.Terminate(userId);

        Assert.False(context.Aborted);
    }

    [Fact]
    public async Task FailingDisconnect_StopsTrackingAndRethrows()
    {
        var userId = Guid.NewGuid();
        var context = await Connect(userId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => terminator.OnDisconnectedAsync(Lifetime(context), null, (_, _) => throw new InvalidOperationException()));
        terminator.Terminate(userId);

        Assert.False(context.Aborted);
    }

    [Fact]
    public async Task FailingConnect_StopsTrackingAndRethrows()
    {
        var userId = Guid.NewGuid();
        var context = FakeHubCallerContext.ForUser(userId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => terminator.OnConnectedAsync(Lifetime(context), _ => throw new InvalidOperationException()));
        terminator.Terminate(userId);

        Assert.False(context.Aborted);
    }

    private async Task<FakeHubCallerContext> Connect(Guid userId)
    {
        var context = FakeHubCallerContext.ForUser(userId);
        await terminator.OnConnectedAsync(Lifetime(context), _ => Task.CompletedTask);
        return context;
    }

    private static HubLifetimeContext Lifetime(FakeHubCallerContext context) =>
        new(context, new ServiceCollection().BuildServiceProvider(), new EmptyHub());

    private class EmptyHub : Hub;
}
