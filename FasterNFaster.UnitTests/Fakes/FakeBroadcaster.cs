using FasterNFaster.Api.UseCases.Interfaces.Realtime;

namespace FasterNFaster.Tests.Fakes;

public class FakeBroadcaster : IBroadcaster
{
    public record Sent(IAudience Audience, string EventName, object? Payload);

    public List<Sent> Broadcasts { get; } = new();

    public Task Broadcast<T>(IAudience audience, string eventName, T payload)
    {
        Broadcasts.Add(new Sent(audience, eventName, payload));
        return Task.CompletedTask;
    }

    public Task Broadcast(IAudience audience, string eventName)
    {
        Broadcasts.Add(new Sent(audience, eventName, null));
        return Task.CompletedTask;
    }
}
