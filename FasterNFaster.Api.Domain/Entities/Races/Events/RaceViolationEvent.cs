using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Entities.Races.Events;

public record RaceViolationEvent(Guid LobbyId, Guid PlayerId, string Nick, string Rule) : IDomainEvent;
