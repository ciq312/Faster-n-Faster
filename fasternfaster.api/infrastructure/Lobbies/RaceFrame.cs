using FasterNFaster.Api.Core.Entities.Races;

namespace FasterNFaster.Api.Infrastructure.Lobbies;

public partial class RaceStateConflator
{
    private sealed record RaceFrame(IReadOnlyList<Guid> PlayerIds, IReadOnlyList<ParticipantSnapshot> Snapshot);
}
