using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Entities.Lobbies.Events;

public record PlayerKickedEvent(Guid UserId, Guid LobbyId, string Nick) : IDomainEvent;