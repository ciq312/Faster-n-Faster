using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Infrastructure.Races;

namespace FasterNFaster.Tests.Services;

public class RaceResultQueueTests
{
    private static RaceParticipantResult Result(int finishPosition) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "nick", 80f, 95f, 1, 10, finishPosition);

    [Fact]
    public async Task Enqueue_ThenDequeue_YieldsSameBatch()
    {
        var queue = new RaceResultQueue();
        var results = new List<RaceParticipantResult> { Result(1) };

        queue.Enqueue(results);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var batch in queue.DequeueAllAsync(cts.Token))
        {
            Assert.Same(results, batch);
            return;
        }

        Assert.Fail("Queue yielded no batch");
    }

    [Fact]
    public async Task Dequeue_PreservesEnqueueOrder()
    {
        var queue = new RaceResultQueue();
        var first = new List<RaceParticipantResult> { Result(1) };
        var second = new List<RaceParticipantResult> { Result(2) };

        queue.Enqueue(first);
        queue.Enqueue(second);

        var received = new List<IReadOnlyList<RaceParticipantResult>>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var batch in queue.DequeueAllAsync(cts.Token))
        {
            received.Add(batch);
            if (received.Count == 2) break;
        }

        Assert.Same(first, received[0]);
        Assert.Same(second, received[1]);
    }
}
