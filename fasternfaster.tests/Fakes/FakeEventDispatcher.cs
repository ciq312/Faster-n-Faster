using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Tests.Fakes;

public class FakeEventDispatcher : IEventDispatcher
{
    public List<IDomainEvent> Dispatched { get; } = new();

    public Task Dispatch<T>(T domainEvent, CancellationToken ct) where T : IDomainEvent
    {
        Dispatched.Add(domainEvent);
        return Task.CompletedTask;
    }
}
