using System.Security.Claims;
using FasterNFaster.Api.Core.Exceptions.Lobbies;
using FasterNFaster.Api.Web.Hubs.Filters;
using FasterNFaster.Tests.Fakes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FasterNFaster.Tests.Hubs;

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
        var context = BuildContext(WithUserId(Guid.NewGuid()));

        var result = await filter.InvokeMethodAsync(context, _ => ValueTask.FromResult<object?>(42));

        Assert.Equal(42, result);
        Assert.Empty(logger.Levels);
    }

    [Fact]
    public async Task StatusException_BecomesHubException_AndDoesNotLog()
    {
        var context = BuildContext(WithUserId(Guid.NewGuid()));

        var ex = await Assert.ThrowsAsync<HubException>(
            () => filter.InvokeMethodAsync(context, _ => throw new NotHostException("start the race")).AsTask());

        Assert.Equal("Only the host can start the race", ex.Message);
        Assert.Empty(logger.Levels);
    }

    [Fact]
    public async Task ExistingHubException_PropagatesUnchanged_AndDoesNotLog()
    {
        var context = BuildContext(WithUserId(Guid.NewGuid()));
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
        var context = BuildContext(WithUserId(userId));
        var thrown = new InvalidOperationException("boom");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => filter.InvokeMethodAsync(context, _ => throw thrown).AsTask());

        Assert.Same(thrown, ex);
        Assert.Equal(LogLevel.Error, Assert.Single(logger.Levels));
        var message = Assert.Single(logger.Messages);
        Assert.Contains(nameof(TestHub.TestMethod), message);
        Assert.Contains(userId.ToString(), message);
        Assert.Same(thrown, Assert.Single(logger.Exceptions));
    }

    [Fact]
    public async Task UnexpectedException_NoResolvableUserId_StillLogs()
    {
        var context = BuildContext(user: null);
        var thrown = new InvalidOperationException("boom");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => filter.InvokeMethodAsync(context, _ => throw thrown).AsTask());

        Assert.Same(thrown, ex);
        Assert.Equal(LogLevel.Error, Assert.Single(logger.Levels));
        var message = Assert.Single(logger.Messages);
        Assert.Contains(nameof(TestHub.TestMethod), message);
    }

    private static ClaimsPrincipal WithUserId(Guid userId) =>
        new(new ClaimsIdentity([new Claim("sub", userId.ToString())]));

    private static HubInvocationContext BuildContext(ClaimsPrincipal? user)
    {
        var callerContext = new FakeHubCallerContext(user);
        var hubMethod = typeof(TestHub).GetMethod(nameof(TestHub.TestMethod))!;
        return new HubInvocationContext(callerContext, serviceProvider: null!, new TestHub(), hubMethod, []);
    }

    private class TestHub : Hub
    {
        public Task TestMethod() => Task.CompletedTask;
    }
}
