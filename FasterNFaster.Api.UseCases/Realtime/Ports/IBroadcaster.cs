namespace FasterNFaster.Api.UseCases.Realtime.Ports;

public interface IBroadcaster
{
    Task Broadcast(IAudience audience, Func<IGameClient, Task> send);
}
