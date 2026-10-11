namespace FasterNFaster.Api.UseCases.Realtime.Ports;

public interface IBroadcaster
{
    Task Broadcast<T>(IAudience audience, string eventName, T payload);
    Task Broadcast(IAudience audience, string eventName);
}
