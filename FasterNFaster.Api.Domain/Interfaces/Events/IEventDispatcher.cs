namespace FasterNFaster.Api.Domain.Interfaces.Events;

public interface IEventDispatcher
{
    Task Dispatch(IDomainEvent domainEvent, CancellationToken ct);
}
