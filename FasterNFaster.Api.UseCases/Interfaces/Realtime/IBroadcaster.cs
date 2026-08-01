namespace FasterNFaster.Api.UseCases.Interfaces.Realtime;

public interface IBroadcaster
{
    Task Broadcast<T>(IAudience audience, string eventName, T payload);
    Task Broadcast(IAudience audience, string eventName);
}
