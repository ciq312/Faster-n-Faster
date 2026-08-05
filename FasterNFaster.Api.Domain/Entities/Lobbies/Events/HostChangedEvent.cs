using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Entities.Lobbies.Events;

public record HostChangedEvent(Guid LobbyId, Guid NewHostId, string NewHostNick) : IDomainEvent;
