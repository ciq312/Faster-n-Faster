using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Lobbies.Events;

public record PlayerRemovedEvent(Guid UserId, Guid LobbyId, string Nick) : IDomainEvent
{
    public Task Dispatch(IEventDispatcher eventDispatcher) => eventDispatcher.Dispatch(this);
}