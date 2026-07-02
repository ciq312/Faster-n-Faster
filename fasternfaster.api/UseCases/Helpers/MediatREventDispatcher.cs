using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Events;
using MediatR;

namespace FasterNFaster.Api.UseCases.Services;

public class MediatREventDispatcher(IServiceScopeFactory scopeFactory) : IEventDispatcher
{
    public async Task Dispatch<T>(T domainEvent, CancellationToken ct) where T : IDomainEvent
    {
        using var scope = scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.Publish(new DomainEventNotification<T>(domainEvent), ct);
    }
}
