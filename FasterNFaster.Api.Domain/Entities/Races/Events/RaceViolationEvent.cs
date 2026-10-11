using FasterNFaster.Api.Domain.Interfaces.Events;

namespace FasterNFaster.Api.Domain.Entities.Races.Events;

public record RaceViolationEvent(Guid LobbyId, Guid PlayerId, string Nick, string Rule) : IDomainEvent;
