using FasterNFaster.Api.Domain.Interfaces.Events;

namespace FasterNFaster.Api.Domain.Entities;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> domainEvents = new();

    public IReadOnlyList<IDomainEvent> DomainEvents => domainEvents;

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => domainEvents.Add(domainEvent);

    public IReadOnlyList<IDomainEvent> DrainEvents()
    {
        if (domainEvents.Count == 0) return [];

        var drained = domainEvents.ToArray();
        domainEvents.Clear();
        return drained;
    }
}
