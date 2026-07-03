using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Helpers;

public class AggregateRootHelper(IEventDispatcher dispatcher) : IAggregateRootHelper
{
    public async Task DispatchRootEventsAsync(AggregateRoot root)
    {
        if (root.DomainEvents.Count == 0) return;

        var events = root.DomainEvents.ToList();
        root.ClearEvents();
        foreach (var domainEvent in events)
            await dispatcher.Dispatch(domainEvent, CancellationToken.None);
    }
}

public interface IAggregateRootHelper
{
    Task DispatchRootEventsAsync(AggregateRoot root);
}
