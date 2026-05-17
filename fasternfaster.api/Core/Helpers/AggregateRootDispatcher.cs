using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Helpers;

public class AggregateRootHelper(IEventDispatcher eventDispatcher) : IAggregateRootHelper
{
    public void DispatchRootEvents(AggregateRoot root)
    {
        foreach (var domainEvent in root.DomainEvents) domainEvent.Dispatch(eventDispatcher);
        root.ClearEvents();
    }
}

public interface IAggregateRootHelper
{
    void DispatchRootEvents(AggregateRoot root);
}