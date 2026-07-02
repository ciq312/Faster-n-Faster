using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Entities.Lobbies.Events;

public record SessionStartedEvent(Guid LobbyId) : IDomainEvent;
