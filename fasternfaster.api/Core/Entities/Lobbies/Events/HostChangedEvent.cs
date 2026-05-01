using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Events;

public record HostChangedEvent(Guid LobbyId, Guid NewHostId, string NewHostNick) : IDomainEvent
{
    public Task Dispatch(IEventDispatcher eventDispatcher) => eventDispatcher.Dispatch(this);

}
