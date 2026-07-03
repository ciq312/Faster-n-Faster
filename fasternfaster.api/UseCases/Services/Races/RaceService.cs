using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Helpers;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Interfaces.Races;

namespace FasterNFaster.Api.UseCases.Services.Races;

public class RaceService(
    IAggregateRootHelper aggregateRootHelper,
    IPassageProvider passageProvider,
    IAntiCheatPolicy antiCheatPolicy,
    ILogger<RaceService> logger) : IRaceService, IRaceInternals
{
    private readonly ConcurrentDictionary<Guid, (SemaphoreSlim, Race)> races = new();

    public Task<List<ParticipantSnapshot>> GetSnapshot(Guid lobbyId) =>
        WithRaceAsync(lobbyId, race => race.GetSnapshot());

    public async Task ProcessUpdate(Guid lobbyId, Guid playerId, int index, int mistakes, string typed)
    {
        Race? race = null;

        await WithRaceAsync(lobbyId, r =>
        {
            r.ProcessUpdate(playerId, index, mistakes, typed, antiCheatPolicy);
            WrapRaceEvents(r, lobbyId);
            race = r;
        });

        if (race != null)
            await aggregateRootHelper.DispatchRootEventsAsync(race);
    }

    public Task StartRace(Guid lobbyId) =>
        WithRaceAsync(lobbyId, race => race.Start());

    public Task AddParticipants(Guid lobbyId, List<RaceParticipant> participants) =>
        WithRaceAsync(lobbyId, race => race.AddParticipants(participants));

    public async Task RefreshPassage(Guid lobbyId)
    {
        var wordCount = await WithRaceAsync(lobbyId, race => race.GetPassageWordCount());
        if (wordCount is null)
            throw new InvalidOperationException("Race type does not support passage refresh");

        var passage = await passageProvider.GetPassageAsync(wordCount.Value);

        await WithRaceAsync(lobbyId, race => race.ApplyPassage(passage));
    }

    public Task WithdrawParticipant(Guid lobbyId, Guid userId) =>
        WithRaceAsync(lobbyId, r => r.WithdrawParticipant(userId));

    public void RegisterRace(Guid lobbyId, Race race)
    {
        logger.LogDebug("New race registered for lobby {LobbyId}", lobbyId);
        races[lobbyId] = (new SemaphoreSlim(1, 1), race);
    }

    public void RemoveRegisteredRace(Guid lobbyId)
    {
        logger.LogDebug("Removing race for lobby {LobbyId}", lobbyId);
        if (races.TryRemove(lobbyId, out var entry))
            entry.Item1.Dispose();
    }

    public async Task<IRaceSettings> GetRaceSettings(Guid lobbyId) =>
        await WithRaceAsync(lobbyId, race => race.GetRaceSettings());

    private (SemaphoreSlim, Race) GetActiveRace(Guid lobbyId)
    {
        var race = races.GetValueOrDefault(lobbyId);
        if (race == default) throw new InvalidOperationException("Race not set");
        return race;
    }

    private bool TryGetActiveRace(Guid lobbyId, out (SemaphoreSlim, Race) race)
    {
        race = races.GetValueOrDefault(lobbyId);
        return race != default;
    }

    private async Task WithRaceAsync(Guid lobbyId, Action<Race> action)
    {
        if (!TryGetActiveRace(lobbyId, out var entry)) return;

        var (gate, race) = entry;
        await gate.WaitAsync();
        try
        {
            action(race);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<T> WithRaceAsync<T>(Guid lobbyId, Func<Race, T> action)
    {
        var (gate, race) = GetActiveRace(lobbyId);
        await gate.WaitAsync();
        try
        {
            return action(race);
        }
        finally
        {
            gate.Release();
        }
    }


    private void WrapRaceEvents(Race race, Guid lobbyId)
    {
        foreach (var domainEvent in race.DomainEvents)
        {
            if (domainEvent is IRaceEvent raceEvent)
                raceEvent.WrapRaceContext(lobbyId);
        }
    }
}
