using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Helpers;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Races;

namespace FasterNFaster.Api.UseCases.Services.Races;

public class RaceService(
    IAggregateRootHelper aggregateRootHelper,
    IPassageProvider passageProvider,
    IAntiCheatPolicy antiCheatPolicy,
    ILogger<RaceService> logger) : IRaceService, IRaceCoordinator
{
    private readonly ConcurrentDictionary<Guid, (ReaderWriterLockSlim, Race)> races = new();

    public Task<List<ParticipantSnapshot>> GetSnapshot(Guid lobbyId) =>
        Task.FromResult(WithRaceRead(lobbyId, race => race.GetSnapshot()));

    public async Task ProcessUpdate(Guid lobbyId, Guid playerId, int index, int mistakes, string typed)
    {
        Race? race = null;

        WithRaceWrite(lobbyId, r =>
        {
            r.ProcessUpdate(playerId, index, mistakes, typed, antiCheatPolicy);
            WrapRaceEvents(r, lobbyId);
            race = r;
        });

        if (race != null)
            await aggregateRootHelper.DispatchRootEventsAsync(race);
    }

    public Task StartRace(Guid lobbyId)
    {
        WithRaceWrite(lobbyId, race => race.Start());
        return Task.CompletedTask;
    }

    public Task AddParticipants(Guid lobbyId, List<RaceParticipant> participants)
    {
        WithRaceWrite(lobbyId, race => race.AddParticipants(participants));
        return Task.CompletedTask;
    }

    public async Task RefreshPassage(Guid lobbyId)
    {
        var wordCount = WithRaceRead(lobbyId, race => race.GetPassageWordCount());
        if (wordCount is null)
            throw new InvalidOperationException("Race type does not support passage refresh");

        var passage = await passageProvider.GetPassageAsync(wordCount.Value);

        WithRaceWrite(lobbyId, race => race.ApplyPassage(passage));
    }

    public Task WithdrawParticipant(Guid lobbyId, Guid userId)
    {
        WithRaceWrite(lobbyId, r => r.WithdrawParticipant(userId));
        return Task.CompletedTask;
    }

    public void RegisterRace(Guid lobbyId, Race race)
    {
        logger.LogDebug("New race registered for lobby {LobbyId}", lobbyId);
        races[lobbyId] = (new ReaderWriterLockSlim(), race);
    }

    public void RemoveRegisteredRace(Guid lobbyId)
    {
        logger.LogDebug("Removing race for lobby {LobbyId}", lobbyId);
        if (races.TryRemove(lobbyId, out var entry))
            entry.Item1.Dispose();
    }

    public IRaceSettings GetRaceSettings(Guid lobbyId) =>
        WithRaceRead(lobbyId, race => race.GetRaceSettings());

    private (ReaderWriterLockSlim, Race) GetActiveRace(Guid lobbyId)
    {
        var race = races.GetValueOrDefault(lobbyId);
        if (race == default) throw new InvalidOperationException("Race not set");
        return race;
    }

    private bool TryGetActiveRace(Guid lobbyId, out (ReaderWriterLockSlim, Race) race)
    {
        race = races.GetValueOrDefault(lobbyId);
        return race != default;
    }

    private void WithRaceWrite(Guid lobbyId, Action<Race> action)
    {
        if (!TryGetActiveRace(lobbyId, out var entry)) return;

        var (gate, race) = entry;
        gate.EnterWriteLock();
        try
        {
            action(race);
        }
        finally
        {
            gate.ExitWriteLock();
        }
    }

    private T WithRaceRead<T>(Guid lobbyId, Func<Race, T> action)
    {
        var (gate, race) = GetActiveRace(lobbyId);
        gate.EnterReadLock();
        try
        {
            return action(race);
        }
        finally
        {
            gate.ExitReadLock();
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
