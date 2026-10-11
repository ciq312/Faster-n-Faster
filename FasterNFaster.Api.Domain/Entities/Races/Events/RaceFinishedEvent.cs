using FasterNFaster.Api.Domain.Interfaces.Events;

namespace FasterNFaster.Api.Domain.Entities.Races.Events;

public record RaceFinishedEvent(Guid LobbyId, IEnumerable<RaceParticipantResult> Results) : IDomainEvent;
