using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Entities.Lobbies.Events;

public record PlayerRemovedEvent(Guid UserId, Guid LobbyId, string Nick) : IDomainEvent;
