using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Helpers;

public class AggregateRootHelper(IEventDispatcher dispatcher) : IAggregateRootHelper
{
    public async Task DispatchRootEventsAsync(AggregateRoot root)
    {
        var events = root.DomainEvents.ToList();
        root.ClearEvents();
        foreach (var domainEvent in events)
            await dispatcher.Dispatch((dynamic)domainEvent, CancellationToken.None);
    }
}

public interface IAggregateRootHelper
{
    Task DispatchRootEventsAsync(AggregateRoot root);
}
