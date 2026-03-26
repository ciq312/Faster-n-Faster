using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Events;

public class EventDispatcher : IEventDispatcher
{
    private readonly IServiceProvider _sp;
    public EventDispatcher(IServiceProvider sp)
    {
        _sp = sp;
    }
    public async Task Dispatch<T>(T domainEvent) where T : IDomainEvent
    {
        var handlers = _sp.GetServices<IDomainEventHandler<T>>();

        foreach (var handler in handlers) await handler.Handle(domainEvent);
    }
}