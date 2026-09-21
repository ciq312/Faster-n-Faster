using FasterNFaster.Api.UseCases.Services;

namespace FasterNFaster.Tests.Services;

public class LobbyStateTrackerTests
{
    [Fact]
    public void MarkChanged_Deduplicates()
    {
        var tracker = new LobbyStateTracker();
        var lobbyId = Guid.NewGuid();

        Assert.True(tracker.TryBeginScope());

        tracker.MarkChanged(lobbyId);
        tracker.MarkChanged(lobbyId);
        tracker.MarkChanged(lobbyId);

        Assert.Equal([lobbyId], tracker.EndScope());
    }

    [Fact]
    public void TryBeginScope_ReturnsFalse_WhenAlreadyOpen()
    {
        var tracker = new LobbyStateTracker();

        Assert.True(tracker.TryBeginScope());
        Assert.False(tracker.TryBeginScope());
    }

    [Fact]
    public void MarkChanged_OutsideScope_IsIgnored()
    {
        var tracker = new LobbyStateTracker();

        tracker.MarkChanged(Guid.NewGuid());

        Assert.Empty(tracker.EndScope());
    }

    [Fact]
    public void EndScope_ClosesScope()
    {
        var tracker = new LobbyStateTracker();

        tracker.TryBeginScope();
        tracker.EndScope();

        Assert.True(tracker.TryBeginScope());
    }

    [Fact]
    public async Task MarkChanged_CollectsEveryId_WhenCalledFromParallelBranches()
    {
        var tracker = new LobbyStateTracker();
        var lobbyIds = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();

        tracker.TryBeginScope();

        await Task.WhenAll(lobbyIds.Select(id => Task.Run(() => tracker.MarkChanged(id))));

        Assert.Equal(lobbyIds.Count, tracker.EndScope().Count);
    }

    [Fact]
    public async Task Scopes_AreIsolated_BetweenConcurrentFlows()
    {
        var tracker = new LobbyStateTracker();

        var flows = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            var lobbyId = Guid.NewGuid();

            tracker.TryBeginScope();
            tracker.MarkChanged(lobbyId);

            return (Expected: lobbyId, Actual: tracker.EndScope());
        }));

        foreach (var (expected, actual) in await Task.WhenAll(flows))
            Assert.Equal([expected], actual);
    }
}
