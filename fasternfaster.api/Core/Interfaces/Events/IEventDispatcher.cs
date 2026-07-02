namespace FasterNFaster.Api.Core.Interfaces.Events;

public interface IEventDispatcher
{
    Task Dispatch<T>(T domainEvent, CancellationToken ct) where T : IDomainEvent;
}
