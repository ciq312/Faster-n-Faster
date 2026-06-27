using FasterNFaster.Api.Core.Entities.Lobbies.Races;

namespace FasterNFaster.Api.UseCases.Interfaces;

public interface IRaceBroadcaster
{
    Task BroadcastRaceStarted(Guid lobbyId);
    Task BroadcastRaceState(IEnumerable<Guid> playerIds, IReadOnlyList<ParticipantSnapshot> snapshot);
}
