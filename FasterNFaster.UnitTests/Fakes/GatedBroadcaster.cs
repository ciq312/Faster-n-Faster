using FasterNFaster.Api.UseCases.Interfaces.Realtime;

namespace FasterNFaster.Tests.Fakes;

public class GatedBroadcaster : IBroadcaster
{
    private readonly SemaphoreSlim releaseGate = new(0);
    private readonly SemaphoreSlim sendStarted = new(0);

    public List<object?> Sent { get; } = new();

    public async Task Broadcast<T>(IAudience audience, string eventName, T payload)
    {
        Sent.Add(payload);
        sendStarted.Release();
        await releaseGate.WaitAsync();
    }

    public Task Broadcast(IAudience audience, string eventName) => Task.CompletedTask;

    public Task WaitForSendStarted() => sendStarted.WaitAsync();

    public void CompleteSend() => releaseGate.Release();
}
