using System.Collections.Concurrent;
using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Interfaces.Events;
using FasterNFaster.Api.UseCases.Races.Ports;
using FasterNFaster.Api.UseCases.Shared;

namespace FasterNFaster.Api.Infrastructure.Races;

public sealed class InMemoryRaceStore : IRaceStore
{
    private readonly ConcurrentDictionary<Guid, Race> races = new();
    private readonly AggregateGate<Race> gate;
    private readonly Func<Guid, Race?> tryGet;
    private readonly ILogger<InMemoryRaceStore> logger;

    public InMemoryRaceStore(IEventDispatcher dispatcher, ILogger<InMemoryRaceStore> logger)
    {
        gate = new AggregateGate<Race>(dispatcher, GetRequired);
        tryGet = races.GetValueOrDefault;
        this.logger = logger;
    }

    public void Add(Race race)
    {
        logger.LogDebug("New race registered for lobby {LobbyId}", race.LobbyId);
        races[race.LobbyId] = race;
    }

    public Task Mutate(Guid lobbyId, Action<Race> mutate) => gate.Mutate(lobbyId, mutate);

    public Task<bool> TryMutate(Guid lobbyId, Action<Race> mutate) => gate.TryMutate(lobbyId, tryGet, mutate);

    public Task<T> Read<T>(Guid lobbyId, Func<Race, T> read) => gate.Read(lobbyId, read);

    public Task<T?> TryRead<T>(Guid lobbyId, Func<Race, T> read) => gate.TryRead(lobbyId, tryGet, read);

    public void Remove(Guid lobbyId)
    {
        logger.LogDebug("Removing race for lobby {LobbyId}", lobbyId);
        races.TryRemove(lobbyId, out _);
        gate.Release(lobbyId);
    }

    public bool Exists(Guid lobbyId) => races.ContainsKey(lobbyId);

    private Race GetRequired(Guid lobbyId) =>
        races.GetValueOrDefault(lobbyId)
        ?? throw new InvalidOperationException($"No race registered for lobby {lobbyId}");
}
