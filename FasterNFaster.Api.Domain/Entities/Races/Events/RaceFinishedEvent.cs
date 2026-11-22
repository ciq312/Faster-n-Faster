using FasterNFaster.Api.Domain.Interfaces.Events;

namespace FasterNFaster.Api.Domain.Entities.Races.Events;

public record RaceFinishedEvent(Guid RaceId, Guid LobbyId, IEnumerable<RaceParticipantResult> Results) : IDomainEvent;
