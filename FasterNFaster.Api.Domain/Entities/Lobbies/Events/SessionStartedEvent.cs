using FasterNFaster.Api.Domain.Interfaces.Events;

namespace FasterNFaster.Api.Domain.Entities.Lobbies.Events;

public record SessionStartedEvent(Guid LobbyId) : IDomainEvent;
