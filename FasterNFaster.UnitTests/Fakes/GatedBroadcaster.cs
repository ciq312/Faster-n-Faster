using FasterNFaster.Api.UseCases.Realtime.Ports;

namespace FasterNFaster.UnitTests.Fakes;

public class GatedBroadcaster : IBroadcaster
{
    private readonly SemaphoreSlim releaseGate = new(0);
    private readonly SemaphoreSlim sendStarted = new(0);

    public List<object?> Sent { get; } = new();

    public async Task Broadcast(IAudience audience, Func<IGameClient, Task> send)
    {
        await send(new RecordingGameClient((_, payload) => Sent.Add(payload)));
        sendStarted.Release();
        await releaseGate.WaitAsync();
    }

    public Task WaitForSendStarted() => sendStarted.WaitAsync();

    public void CompleteSend() => releaseGate.Release();
}
