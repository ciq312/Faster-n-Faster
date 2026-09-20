using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Entities.Races.Events;

public record RaceFinishedEvent(Guid LobbyId, IEnumerable<RaceParticipantResult> Results) : IDomainEvent;