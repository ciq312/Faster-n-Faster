using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Interfaces.Events;
using MediatR;

namespace FasterNFaster.Api.Core.Helpers;

public class AggregateRootHelper(IPublisher publisher) : IAggregateRootHelper
{
    public async Task DispatchRootEventsAsync(AggregateRoot root)
    {
        var events = root.DomainEvents.ToList();
        root.ClearEvents();
        foreach (var domainEvent in events)
            await publisher.Publish(domainEvent);
    }
}

public interface IAggregateRootHelper
{
    Task DispatchRootEventsAsync(AggregateRoot root);
}
