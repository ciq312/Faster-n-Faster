namespace FasterNFaster.Api.Core.Interfaces.Events;

public interface IEventDispatcher
{
    public Task Dispatch<T>(T domainEvent) where T : IDomainEvent;
}