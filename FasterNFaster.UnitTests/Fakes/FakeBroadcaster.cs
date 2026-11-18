using FasterNFaster.Api.UseCases.Realtime.Ports;

namespace FasterNFaster.UnitTests.Fakes;

public class FakeBroadcaster : IBroadcaster
{
    public record Sent(IAudience Audience, string EventName, object? Payload);

    public List<Sent> Broadcasts { get; } = new();

    public Task Broadcast(IAudience audience, Func<IGameClient, Task> send) =>
        send(new RecordingGameClient((eventName, payload) => Broadcasts.Add(new Sent(audience, eventName, payload))));
}
