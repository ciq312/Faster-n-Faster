using FasterNFaster.Api.Domain.Interfaces.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FasterNFaster.Api.UseCases.Shared;

public class MediatREventDispatcher(IServiceScopeFactory scopeFactory) : IEventDispatcher
{
    public async Task Dispatch(IDomainEvent domainEvent, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // MediatR resolves handlers by the notification's constructed type, so the wrapper is built from the event's runtime type.
        // dynamic is preferred over double dispatch, which would force a Dispatch method onto every event.
        INotification notification = CreateNotification((dynamic)domainEvent);
        await publisher.Publish(notification, ct);
    }

    private static DomainEventNotification<T> CreateNotification<T>(T domainEvent) where T : IDomainEvent
        => new(domainEvent);
}
