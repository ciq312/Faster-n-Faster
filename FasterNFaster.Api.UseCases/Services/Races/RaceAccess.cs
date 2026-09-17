using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Interfaces.Races;

namespace FasterNFaster.Api.UseCases.Services.Races;

public class RaceAccess(
    IEventDispatcher eventDispatcher,
    IPassageProvider passageProvider,
    IAntiCheatPolicy antiCheatPolicy,
    ILogger<RaceAccess> logger) : IRaceAccess
{
    private readonly ConcurrentDictionary<Guid, (SemaphoreSlim Gate, Race Race)> races = new();

    public async Task Mutate(Guid lobbyId, Action<Race> mutate)
    {
        List<IDomainEvent> events = [];

        await WithGate(GetRequired(lobbyId), race =>
        {
            mutate(race);
            WrapRaceEvents(race, lobbyId);
            events = [.. race.DomainEvents];
            race.ClearEvents();
            return true;
        });

        foreach (var domainEvent in events)
            await eventDispatcher.Dispatch(domainEvent, CancellationToken.None);
    }

    public Task ProcessUpdate(Guid lobbyId, Guid playerId, int index, int mistakes, string typed) =>
        Mutate(lobbyId, race => race.ProcessUpdate(playerId, index, mistakes, typed, antiCheatPolicy));

    public async Task RefreshPassage(Guid lobbyId)
    {
        var wordCount = await Read(lobbyId, race => race.GetPassageWordCount());
        if (wordCount is null)
            throw new InvalidOperationException("Race type does not support passage refresh");

        var passage = await passageProvider.GetPassageAsync(wordCount.Value);

        await Mutate(lobbyId, race => race.ApplyPassage(passage));
    }

    public Task<List<ParticipantSnapshot>> GetSnapshot(Guid lobbyId) =>
        Read(lobbyId, race => race.GetSnapshot());

    public Task<IRaceSettings> GetRaceSettings(Guid lobbyId) =>
        Read(lobbyId, race => race.GetRaceSettings());

    public async Task<IRaceSettings?> GetRaceSettingsOrDefault(Guid lobbyId)
    {
        if (!races.TryGetValue(lobbyId, out var entry)) return null;

        return await WithGate(entry, race => race.GetRaceSettings());
    }

    public void Register(Guid lobbyId, Race race)
    {
        logger.LogDebug("New race registered for lobby {LobbyId}", lobbyId);
        races[lobbyId] = (new SemaphoreSlim(1, 1), race);
    }

    // The semaphore is deliberately not disposed: in-flight callers (tick snapshots,
    // sibling disconnect handlers) hold a copy of the tuple and may still Wait/Release
    // on it. SemaphoreSlim owns no OS handle here, so GC collects it safely; disposing
    // would fault or hang those callers.
    public void Remove(Guid lobbyId)
    {
        logger.LogDebug("Removing race for lobby {LobbyId}", lobbyId);
        races.TryRemove(lobbyId, out _);
    }

    private Task<T> Read<T>(Guid lobbyId, Func<Race, T> read) => WithGate(GetRequired(lobbyId), read);

    private static async Task<T> WithGate<T>((SemaphoreSlim Gate, Race Race) entry, Func<Race, T> action)
    {
        await entry.Gate.WaitAsync();
        try
        {
            return action(entry.Race);
        }
        finally
        {
            entry.Gate.Release();
        }
    }

    private (SemaphoreSlim Gate, Race Race) GetRequired(Guid lobbyId)
    {
        if (!races.TryGetValue(lobbyId, out var entry))
            throw new InvalidOperationException($"No race registered for lobby {lobbyId}");

        return entry;
    }

    private static void WrapRaceEvents(Race race, Guid lobbyId)
    {
        foreach (var domainEvent in race.DomainEvents)
        {
            if (domainEvent is IRaceEvent raceEvent)
                raceEvent.WrapRaceContext(lobbyId);
        }
    }
}
