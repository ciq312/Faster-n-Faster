using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Events;
using MediatR;

namespace FasterNFaster.Api.UseCases.Services;

public class MediatREventDispatcher(IServiceScopeFactory scopeFactory) : IEventDispatcher
{
    public async Task Dispatch(IDomainEvent domainEvent, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // MediatR matches handlers by the notification's constructed generic type, so the
        // wrapper must be built with the event's runtime type — hence the dynamic hop.
        // The typed variable keeps Publish and the await statically bound.
        // Instead of using dynamic double dispatching could have been used,
        // but that would require every event to implement a Dispatch method, which is more boilerplate and less discoverable.
        INotification notification = CreateNotification((dynamic)domainEvent);
        await publisher.Publish(notification, ct);
    }

    private static DomainEventNotification<T> CreateNotification<T>(T domainEvent) where T : IDomainEvent
        => new(domainEvent);
}
