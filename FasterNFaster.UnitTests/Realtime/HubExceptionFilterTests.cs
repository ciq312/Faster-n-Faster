using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.Presentation.Web.Realtime.Filters;
using FasterNFaster.UnitTests.Fakes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FasterNFaster.UnitTests.Realtime;

public class HubExceptionFilterTests
{
    private readonly FakeLogger<HubExceptionFilter> logger = new();
    private readonly HubExceptionFilter filter;

    public HubExceptionFilterTests()
    {
        filter = new HubExceptionFilter(logger);
    }

    [Fact]
    public async Task Success_DoesNotLog()
    {
        var context = BuildContext(FakeHubCallerContext.ForUser(Guid.NewGuid()));

        var result = await filter.InvokeMethodAsync(context, _ => ValueTask.FromResult<object?>(42));

        Assert.Equal(42, result);
        Assert.Empty(logger.Levels);
    }

    [Fact]
    public async Task StatusException_BecomesHubException_AndDoesNotLog()
    {
        var context = BuildContext(FakeHubCallerContext.ForUser(Guid.NewGuid()));

        var ex = await Assert.ThrowsAsync<HubException>(
            () => filter.InvokeMethodAsync(context, _ => throw new NotHostException("start the race")).AsTask());

        Assert.Equal("Only the host can start the race", ex.Message);
        Assert.Empty(logger.Levels);
    }

    [Fact]
    public async Task ExistingHubException_PropagatesUnchanged_AndDoesNotLog()
    {
        var context = BuildContext(FakeHubCallerContext.ForUser(Guid.NewGuid()));
        var thrown = new HubException("Not authenticated.");

        var ex = await Assert.ThrowsAsync<HubException>(
            () => filter.InvokeMethodAsync(context, _ => throw thrown).AsTask());

        Assert.Same(thrown, ex);
        Assert.Empty(logger.Levels);
    }

    [Fact]
    public async Task UnexpectedException_RethrowsUnchanged_AndLogsWithHubMethodAndUserId()
    {
        var userId = Guid.NewGuid();
        var context = BuildContext(FakeHubCallerContext.ForUser(userId));
        var thrown = new InvalidOperationException("boom");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => filter.InvokeMethodAsync(context, _ => throw thrown).AsTask());

        Assert.Same(thrown, ex);
        Assert.Equal(2, logger.Levels.Count);
        Assert.All(logger.Levels, level => Assert.Equal(LogLevel.Error, level));
        Assert.Contains(logger.Messages, message => message.Contains(nameof(TestHub.TestMethod)) && message.Contains(userId.ToString()));
        Assert.Contains(logger.Messages, message => message.Contains("signalr.error"));
        Assert.Same(thrown, Assert.Single(logger.Exceptions));
    }

    [Fact]
    public async Task UnexpectedException_NoResolvableUserId_StillLogs()
    {
        var context = BuildContext(new FakeHubCallerContext(null));
        var thrown = new InvalidOperationException("boom");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => filter.InvokeMethodAsync(context, _ => throw thrown).AsTask());

        Assert.Same(thrown, ex);
        Assert.Equal(2, logger.Levels.Count);
        Assert.All(logger.Levels, level => Assert.Equal(LogLevel.Error, level));
        Assert.Contains(logger.Messages, message => message.Contains(nameof(TestHub.TestMethod)));
        Assert.Contains(logger.Messages, message => message.Contains("signalr.error"));
    }

    private static HubInvocationContext BuildContext(FakeHubCallerContext callerContext)
    {
        var hubMethod = typeof(TestHub).GetMethod(nameof(TestHub.TestMethod))!;
        return new HubInvocationContext(callerContext, serviceProvider: null!, new TestHub(), hubMethod, []);
    }

    private class TestHub : Hub
    {
        public Task TestMethod() => Task.CompletedTask;
    }
}
