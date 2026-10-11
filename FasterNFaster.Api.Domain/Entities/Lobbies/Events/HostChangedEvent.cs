using FasterNFaster.Api.Domain.Interfaces.Events;

namespace FasterNFaster.Api.Domain.Entities.Lobbies.Events;

public record HostChangedEvent(Guid LobbyId, Guid NewHostId, string NewHostNick) : IDomainEvent;
