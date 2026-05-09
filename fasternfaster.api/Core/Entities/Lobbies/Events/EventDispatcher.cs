using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Events;

public class EventDispatcher(IServiceScopeFactory scopeFactory) : IEventDispatcher
{
    public async Task Dispatch<T>(T domainEvent) where T : IDomainEvent
    {
        using var scope = scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IDomainEventHandler<T>>();
#if DEBUG
        Log.Information($"dispatching event {domainEvent.GetType()}");
#endif
        foreach (var handler in handlers) await handler.Handle(domainEvent);
    }
}