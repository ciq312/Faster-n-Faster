namespace FasterNFaster.Api.Core.Interfaces.Events;

public interface IDomainEventHandler<T> where T : IDomainEvent
{
    public Task Handle(T domainEvent);
}