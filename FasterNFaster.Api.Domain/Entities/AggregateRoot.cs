using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Entities;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> domainEvents = new();

    public IReadOnlyList<IDomainEvent> DomainEvents => domainEvents;

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => domainEvents.Add(domainEvent);

    public IReadOnlyList<IDomainEvent> DrainEvents()
    {
        var drained = domainEvents.ToList();
        domainEvents.Clear();
        return drained;
    }
}