using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Events;
using MediatR;

namespace FasterNFaster.Api.UseCases.Services;

public class MediatREventDispatcher(IPublisher publisher) : IEventDispatcher
{
    public Task Dispatch<T>(T domainEvent, CancellationToken ct) where T : IDomainEvent =>
        publisher.Publish(new DomainEventNotification<T>(domainEvent), ct);
}
