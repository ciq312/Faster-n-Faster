using System.Threading.Channels;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.UseCases.Interfaces.Races;

namespace FasterNFaster.Api.Infrastructure.Races;

public class RaceResultQueue : IRaceResultQueue
{
    private readonly Channel<IReadOnlyList<RaceParticipantResult>> channel =
        Channel.CreateUnbounded<IReadOnlyList<RaceParticipantResult>>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

    public void Enqueue(IEnumerable<RaceParticipantResult> results) =>
        channel.Writer.TryWrite(results as IReadOnlyList<RaceParticipantResult> ?? results.ToList());

    public IAsyncEnumerable<IReadOnlyList<RaceParticipantResult>> DequeueAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}
