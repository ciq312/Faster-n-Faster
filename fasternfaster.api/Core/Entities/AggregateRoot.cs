using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Entities;

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> domainEvents = new();

    public IReadOnlyList<IDomainEvent> DomainEvents => domainEvents;

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => domainEvents.Add(domainEvent);

    public void ClearEvents() => domainEvents.Clear();
}