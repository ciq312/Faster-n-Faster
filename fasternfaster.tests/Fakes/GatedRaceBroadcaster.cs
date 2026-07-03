using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Tests.Fakes;

public class GatedRaceBroadcaster : IRaceBroadcaster
{
    private readonly SemaphoreSlim releaseGate = new(0);
    private readonly SemaphoreSlim sendStarted = new(0);

    public List<IReadOnlyList<ParticipantSnapshot>> Sent { get; } = new();

    public Task BroadcastRaceStarted(Guid lobbyId) => Task.CompletedTask;

    public async Task BroadcastRaceState(IEnumerable<Guid> playerIds, IReadOnlyList<ParticipantSnapshot> snapshot)
    {
        Sent.Add(snapshot);
        sendStarted.Release();
        await releaseGate.WaitAsync();
    }

    public Task WaitForSendStarted() => sendStarted.WaitAsync();

    public void CompleteSend() => releaseGate.Release();
}
