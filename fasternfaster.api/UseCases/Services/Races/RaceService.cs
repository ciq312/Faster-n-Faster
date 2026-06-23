using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Helpers;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using static FasterNFaster.Api.Core.Entities.Lobbies.Races.WordRace;

namespace FasterNFaster.Api.UseCases.Services.Races;

public class RaceService(IAggregateRootHelper aggregateRootHelper,
 IPassageProvider passageProvider,
 ILogger<RaceService> logger
 ) : IRaceService, IRaceCoordinator
{
    private readonly ConcurrentDictionary<Guid, (ReaderWriterLockSlim, Race)> races = new();
    private readonly IPassageProvider passageProvider = passageProvider;
    private readonly ILogger<RaceService> logger = logger;
    public Task<List<ParticipantSnapshot>> GetSnapshot(Guid lobbyId)
    {
        return Task.FromResult(WithRaceRead(lobbyId, race => race.GetSnapshot()));
    }

    public Task ProcessUpdate(Guid lobbyId, Guid playerdId, int index, int mistakes, string typed)
    {
        WithRaceWrite(lobbyId, race =>
        {
            race.ProcessUpdate(playerdId, index, mistakes, typed);

            WrapRaceEvents(race, lobbyId);

            aggregateRootHelper.DispatchRootEvents(race);
        });
        return Task.CompletedTask;
    }

    public Task StartRace(Guid lobbyId)
    {
        WithRaceWrite(lobbyId, race => race.Start());
        return Task.CompletedTask;
    }
    public Task AddPaticipants(Guid lobbyId, List<RaceParticipant> participants)
    {
        WithRaceWrite(lobbyId, race => race.AddParticipants(participants));
        return Task.CompletedTask;
    }
    public async Task RefreshPassage(Guid lobbyId)
    {
        var raceSettings = await GetRaceSettings(lobbyId) as WordRaceSettings;

        if (raceSettings is null)
            throw new InvalidOperationException("Can't refresh passage: wrong race type");

        var passage = await passageProvider.GetPassageAsync(raceSettings.WordCount);

        WithRaceWrite(lobbyId, race =>
        {
            var wordRace = race as WordRace ?? throw new InvalidOperationException("Can't refresh passage: wrong race type");
            wordRace.SetPassage(passage);
        });
    }
    public Task WithdrawParticipant(Guid lobbyId, Guid userId)
    {
        WithRaceWrite(lobbyId, r => r.WithdrawParticipant(userId));
        return Task.CompletedTask;
    }
    public void RegisterRace(Guid lobbyId, Race race)
    {
        logger.LogDebug($"new race registered for lobby {lobbyId}");
        races[lobbyId] = (new ReaderWriterLockSlim(), race);
    }
    public void RemoveRegistredRace(Guid lobbyId)
    {
        logger.LogDebug($"removing race for lobby {lobbyId}");
        if (races.TryRemove(lobbyId, out var entry))
            entry.Item1.Dispose();
    }

    private (ReaderWriterLockSlim, Race) GetActiveRace(Guid lobbyId)
    {
        var race = races.GetValueOrDefault(lobbyId);
        if (race == default) throw new InvalidOperationException("Race not set");
        return race;
    }

    private bool TryGetActiveRace(Guid lobbyId, out (ReaderWriterLockSlim, Race) race)
    {
        race = races.GetValueOrDefault(lobbyId);
        if (race == default) return false;
        return true;
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


    private void WrapRaceEvents(Race race, Guid LobbyId)
    {
        foreach (var domainEvent in race.DomainEvents)
        {
            if (domainEvent is IRaceEvent) WrapRaceEvent(LobbyId, (IRaceEvent)domainEvent);
        }
    }
    private void WrapRaceEvent(Guid lobbyId, IRaceEvent raceEvent)
    {
        raceEvent.WrapRaceContext(lobbyId);
    }

    public Task<IRaceSettings> GetRaceSettings(Guid lobbyId)
    {
        return Task.FromResult(WithRaceRead(lobbyId, race => race.GetRaceSettings()));
    }

    public Type GetRaceType(Guid lobbyId)
    {
        var (_, race) = GetActiveRace(lobbyId);
        return race.GetType();
    }
}
