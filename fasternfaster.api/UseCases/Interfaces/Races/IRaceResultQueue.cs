using FasterNFaster.Api.Core.Entities.Lobbies.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IRaceResultQueue
{
    void Enqueue(IEnumerable<RaceParticipantResult> results);

    IAsyncEnumerable<IReadOnlyList<RaceParticipantResult>> DequeueAllAsync(CancellationToken cancellationToken);
}
