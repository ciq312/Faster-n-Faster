using FasterNFaster.Api.Domain.Interfaces.Events;

namespace FasterNFaster.Api.Domain.Entities.Lobbies.Events;

public record PlayerJoinedEvent(Guid UserId, Guid LobbyId, string Nick) : IDomainEvent;
