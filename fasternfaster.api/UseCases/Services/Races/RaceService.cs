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
    private readonly ConcurrentDictionary<Guid, (SemaphoreSlim, Race)> races = new();
    private readonly IPassageProvider passageProvider = passageProvider;
    private readonly ILogger<RaceService> logger = logger;
    public async Task<List<ParticipantSnapshot>> GetSnapshot(Guid lobbyId)
    {
        var (gate, race) = GetActiveRace(lobbyId);

        return await WithRace(lobbyId, race => race.GetSnapshot());
    }

    public async Task ProcessUpdate(Guid lobbyId, Guid playerdId, int index, int mistakes, string typed)
    {
        await WithRace(lobbyId, race =>
        {
            race.ProcessUpdate(playerdId, index, mistakes, typed);

            WrapRaceEvents(race, lobbyId);

            aggregateRootHelper.DispatchRootEvents(race);
        });
    }

    public async Task StartRace(Guid lobbyId)
    {
        await WithRace(lobbyId, race =>
        {
            race.Start();
        });
    }
    public async Task AddPaticipants(Guid lobbyId, List<RaceParticipant> participants)
    {
        await WithRace(lobbyId, race =>
        {
            race.AddParticipants(participants);
        });
    }
    public async Task RefreshPassage(Guid lobbyId)
    {
        var raceSettings = await GetRaceSettings(lobbyId) as WordRaceSettings;

        if (raceSettings is null)
            throw new InvalidOperationException("Can't refresh passage: wrong race type");

        var passage = await passageProvider.GetPassageAsync(raceSettings.WordCount);

        await WithRace(lobbyId, async race =>
        {
            var wordRace = race as WordRace ?? throw new InvalidOperationException("Can't refresh passage: wrong race type");
            wordRace.SetPassage(passage);
        });
    }
    public async Task WithdrawParticipant(Guid lobbyId, Guid userId)
    {
        await WithRace(lobbyId, r => r.WithdrawParticipant(userId));
    }
    public void RegisterRace(Guid lobbyId, Race race)
    {
        logger.LogDebug($"new race registered for lobby {lobbyId}");
        races[lobbyId] = (new SemaphoreSlim(1, 1), race);
    }
    public void RemoveRegistredRace(Guid lobbyId)
    {
        logger.LogDebug($"removing race for lobby {lobbyId}");
        if (races.TryRemove(lobbyId, out var sem))
            sem.Item1.Dispose();

    }

    private (SemaphoreSlim, Race) GetActiveRace(Guid lobbyId)
    {
        var race = races.GetValueOrDefault(lobbyId);
        if (race == default) throw new NullReferenceException("Race not set");
        return race;
    }

    private async Task WithRace(Guid lobbyId, Action<Race> action)
    {
        var (gate, race) = GetActiveRace(lobbyId);

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
    private async Task<T> WithRace<T>(Guid lobbyId, Func<Race, T> action)
    {

        var (gate, race) = GetActiveRace(lobbyId);

        await gate.WaitAsync();

        try
        {
            var result = action(race);
            return result;
        }

        finally
        {
            gate.Release();
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

    public async Task<IRaceSettings> GetRaceSettings(Guid lobbyId)
    {
        return await WithRace(lobbyId, race => race.GetRaceSettings());
    }

    public Type GetRaceType(Guid lobbyId)
    {
        var (_, race) = GetActiveRace(lobbyId);
        return race.GetType();
    }
}