namespace FasterNFaster.Api.Core.Interfaces.Events;

public interface IDomainEvent
{
    Task Dispatch(IEventDispatcher eventDispatcher);
}
