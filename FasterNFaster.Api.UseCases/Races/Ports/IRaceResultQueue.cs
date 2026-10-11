using FasterNFaster.Api.Domain.Entities.Races;

namespace FasterNFaster.Api.UseCases.Races.Ports;

public interface IRaceResultQueue
{
    void Enqueue(IEnumerable<RaceParticipantResult> results);

    IAsyncEnumerable<IReadOnlyList<RaceParticipantResult>> DequeueAllAsync(CancellationToken cancellationToken);
}
