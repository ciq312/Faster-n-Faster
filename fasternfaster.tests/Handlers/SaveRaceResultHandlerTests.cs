using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Entities.Lobbies.Races.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Handlers;

public class SaveRaceResultHandlerTests
{
    [Fact]
    public async Task Handle_EnqueuesEventResults()
    {
        var queue = new FakeRaceResultQueue();
        var handler = new SaveRaceResultHandler(queue);
        var results = new List<RaceParticipantResult>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "nick", 80f, 95f, 1, 10, 1)
        };
        var e = new RaceFinishedEvent(results);

        await handler.Handle(new DomainEventNotification<RaceFinishedEvent>(e), CancellationToken.None);

        Assert.Same(results, Assert.Single(queue.Enqueued));
    }
}
