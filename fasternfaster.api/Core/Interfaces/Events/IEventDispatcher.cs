namespace FasterNFaster.Api.Core.Interfaces.Events;

public interface IEventDispatcher
{
    Task Dispatch(IDomainEvent domainEvent, CancellationToken ct);
}
