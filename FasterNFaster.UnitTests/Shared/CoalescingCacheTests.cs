using System.Collections.Concurrent;
using FasterNFaster.Api.Infrastructure.Shared.Caching;
using Moq;
using Shouldly;

namespace FasterNFaster.UnitTests.Shared;

public class CoalescingCacheTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(1);

    private readonly Mock<ICache> inner = new();
    private readonly CoalescingCache sut;

    public CoalescingCacheTests()
    {
        sut = new CoalescingCache(inner.Object);
    }

    [Fact]
    public async Task ConcurrentMissesOnOneKey_CallInnerOnceAndShareItsValue()
    {
        var held = HoldLoad("a");

        var calls = Enumerable.Range(0, 10).Select(_ => Call("a")).ToList();
        held.SetResult("value");

        (await Task.WhenAll(calls)).ShouldAllBe(value => value == "value");
        VerifyInnerCalls("a", Times.Once());
    }

    [Fact]
    public async Task ConcurrentMissesFromManyThreads_CallInnerOnce()
    {
        var held = HoldLoad("a");
        var calls = new ConcurrentBag<Task<string>>();

        await ConcurrentRunner.RunTogether(Enumerable.Range(0, 50).Select<int, Func<Task>>(_ => () =>
        {
            calls.Add(Call("a"));
            return Task.CompletedTask;
        }));
        held.SetResult("value");

        (await Task.WhenAll(calls)).ShouldAllBe(value => value == "value");
        VerifyInnerCalls("a", Times.Once());
    }

    [Fact]
    public async Task HeldLoadOnOneKey_DoesNotBlockOtherKey()
    {
        var held = HoldLoad("a");
        inner.Setup(c => c.GetOrSetAsync("b", It.IsAny<Func<Task<string>>>(), Ttl)).ReturnsAsync("b");
        var blocked = Call("a");

        var other = await Call("b").WaitAsync(TimeSpan.FromSeconds(1));

        other.ShouldBe("b");
        blocked.IsCompleted.ShouldBeFalse();
        held.SetResult("value");
        await blocked;
    }

    [Fact]
    public async Task FailedLoad_NextCallRunsInnerAgain()
    {
        inner.SetupSequence(c => c.GetOrSetAsync("a", It.IsAny<Func<Task<string>>>(), Ttl))
            .ThrowsAsync(new InvalidOperationException())
            .ReturnsAsync("value");

        await Should.ThrowAsync<InvalidOperationException>(Call("a"));
        var value = await Call("a");

        value.ShouldBe("value");
        VerifyInnerCalls("a", Times.Exactly(2));
    }

    [Fact]
    public async Task SynchronouslyThrowingInner_NextCallRunsInnerAgain()
    {
        inner.SetupSequence(c => c.GetOrSetAsync("a", It.IsAny<Func<Task<string>>>(), Ttl))
            .Throws(new InvalidOperationException())
            .ReturnsAsync("value");

        await Should.ThrowAsync<InvalidOperationException>(Call("a"));
        var value = await Call("a");

        value.ShouldBe("value");
        VerifyInnerCalls("a", Times.Exactly(2));
    }

    [Fact]
    public async Task ConcurrentMissesOnFailingLoad_AllReceiveTheException()
    {
        var held = HoldLoad("a");
        var calls = Enumerable.Range(0, 10).Select(_ => Call("a")).ToList();

        held.SetException(new InvalidOperationException());

        foreach (var call in calls)
            await Should.ThrowAsync<InvalidOperationException>(call);
        VerifyInnerCalls("a", Times.Once());
    }

    [Fact]
    public async Task CompletedLoad_NextCallRunsInnerAgain()
    {
        inner.Setup(c => c.GetOrSetAsync("a", It.IsAny<Func<Task<string>>>(), Ttl)).ReturnsAsync("value");

        await Call("a");
        await Call("a");

        VerifyInnerCalls("a", Times.Exactly(2));
    }

    [Fact]
    public async Task HeldLoadCompleted_NextCallRunsInnerAgain()
    {
        var held = HoldLoad("a");
        var first = Call("a");
        held.SetResult("value");
        await first;

        await Call("a");

        VerifyInnerCalls("a", Times.Exactly(2));
    }

    [Fact]
    public async Task Miss_ForwardsKeyFactoryAndTtlToInner()
    {
        inner.Setup(c => c.GetOrSetAsync("a", It.IsAny<Func<Task<string>>>(), Ttl)).ReturnsAsync("value");
        Func<Task<string>> factory = () => Task.FromResult("value");

        await sut.GetOrSetAsync("a", factory, Ttl);

        inner.Verify(c => c.GetOrSetAsync("a", factory, Ttl), Times.Once);
    }

    private TaskCompletionSource<string> HoldLoad(string key)
    {
        var held = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        inner.Setup(c => c.GetOrSetAsync(key, It.IsAny<Func<Task<string>>>(), Ttl)).Returns(held.Task);
        return held;
    }

    private Task<string> Call(string key) => sut.GetOrSetAsync(key, () => Task.FromResult("fresh"), Ttl);

    private void VerifyInnerCalls(string key, Times times) =>
        inner.Verify(c => c.GetOrSetAsync(key, It.IsAny<Func<Task<string>>>(), Ttl), times);
}
