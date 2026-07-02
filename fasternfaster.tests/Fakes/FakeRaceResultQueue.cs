using System.Runtime.CompilerServices;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.UseCases.Interfaces.Races;

namespace FasterNFaster.Tests.Fakes;

public class FakeRaceResultQueue : IRaceResultQueue
{
    public List<IEnumerable<RaceParticipantResult>> Enqueued { get; } = new();

    public void Enqueue(IEnumerable<RaceParticipantResult> results) => Enqueued.Add(results);

    public async IAsyncEnumerable<IReadOnlyList<RaceParticipantResult>> DequeueAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        yield break;
    }
}
