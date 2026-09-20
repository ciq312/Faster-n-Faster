using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Interfaces.Races;

namespace FasterNFaster.Api.UseCases.Services.Races;

public class RaceAccess : IRaceAccess
{
    private readonly ConcurrentDictionary<Guid, Race> races = new();
    private readonly AggregateGate<Race> gate;
    private readonly IPassageProvider passageProvider;
    private readonly IAntiCheatPolicy antiCheatPolicy;
    private readonly ILogger<RaceAccess> logger;

    public RaceAccess(
        IEventDispatcher eventDispatcher,
        IPassageProvider passageProvider,
        IAntiCheatPolicy antiCheatPolicy,
        ILogger<RaceAccess> logger)
    {
        gate = new AggregateGate<Race>(eventDispatcher, GetRequired);
        this.passageProvider = passageProvider;
        this.antiCheatPolicy = antiCheatPolicy;
        this.logger = logger;
    }

    public Task Mutate(Guid lobbyId, Action<Race> mutate) => gate.Mutate(lobbyId, mutate);

    public Task ProcessUpdate(Guid lobbyId, Guid playerId, int index, int mistakes, string typed) =>
        Mutate(lobbyId, race => race.ProcessUpdate(playerId, index, mistakes, typed, antiCheatPolicy));

    public async Task RefreshPassage(Guid lobbyId)
    {
        var wordCount = await gate.Read(lobbyId, race => race.GetPassageWordCount());
        if (wordCount is null)
            throw new InvalidOperationException("Race type does not support passage refresh");

        var passage = await passageProvider.GetPassageAsync(wordCount.Value);

        await Mutate(lobbyId, race => race.ApplyPassage(passage));
    }

    public Task<List<ParticipantSnapshot>> GetSnapshot(Guid lobbyId) =>
        gate.Read(lobbyId, race => race.GetSnapshot());

    public Task<IRaceSettings> GetRaceSettings(Guid lobbyId) =>
        gate.Read(lobbyId, race => race.GetRaceSettings());

    public Task<IRaceSettings?> GetRaceSettingsOrDefault(Guid lobbyId) =>
        gate.TryRead(lobbyId, TryGet, race => race.GetRaceSettings());

    public void Register(Race race)
    {
        logger.LogDebug("New race registered for lobby {LobbyId}", race.LobbyId);
        races[race.LobbyId] = race;
    }

    public void Remove(Guid lobbyId)
    {
        logger.LogDebug("Removing race for lobby {LobbyId}", lobbyId);
        races.TryRemove(lobbyId, out _);
        gate.Release(lobbyId);
    }

    private Race? TryGet(Guid lobbyId) => races.GetValueOrDefault(lobbyId);

    private Race GetRequired(Guid lobbyId) =>
        races.GetValueOrDefault(lobbyId)
        ?? throw new InvalidOperationException($"No race registered for lobby {lobbyId}");
}
