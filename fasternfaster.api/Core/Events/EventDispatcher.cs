using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Events;

public class EventDispatcher(IServiceProvider sp) : IEventDispatcher
{
    private readonly IServiceProvider sp = sp;

    public async Task Dispatch<T>(T domainEvent) where T : IDomainEvent
    {
        var handlers = sp.GetServices<IDomainEventHandler<T>>();

        foreach (var handler in handlers) await handler.Handle(domainEvent);
    }
}