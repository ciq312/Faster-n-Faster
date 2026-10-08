using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Tests.Fakes;

public class FakeEventDispatcher : IEventDispatcher
{
    public ConcurrentQueue<IDomainEvent> Dispatched { get; } = new();

    public Func<IDomainEvent, Task>? OnDispatch { get; set; }

    public async Task Dispatch(IDomainEvent domainEvent, CancellationToken ct)
    {
        Dispatched.Enqueue(domainEvent);
        if (OnDispatch is not null)
            await OnDispatch(domainEvent);
    }
}
