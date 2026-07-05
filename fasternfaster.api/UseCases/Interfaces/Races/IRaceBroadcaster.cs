using FasterNFaster.Api.Core.Entities.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IRaceBroadcaster
{
    Task BroadcastRaceStarted(Guid lobbyId);
    Task BroadcastRaceState(IEnumerable<Guid> playerIds, IReadOnlyList<ParticipantSnapshot> snapshot);
}
