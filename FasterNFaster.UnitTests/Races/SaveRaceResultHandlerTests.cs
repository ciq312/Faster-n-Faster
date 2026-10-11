using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Races.SaveRaceResults;
using FasterNFaster.Api.UseCases.Shared;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Races;

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
        var e = new RaceFinishedEvent(Guid.NewGuid(), results);

        await handler.Handle(new DomainEventNotification<RaceFinishedEvent>(e), CancellationToken.None);

        Assert.Same(results, Assert.Single(queue.Enqueued));
    }
}
