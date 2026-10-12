using FasterNFaster.Api.Domain.Entities.Races;

namespace FasterNFaster.Api.UseCases.Races.Ports;

public interface IRaceStore
{
    void Add(Race race);
    Task Mutate(Guid lobbyId, Action<Race> mutate);
    Task<bool> TryMutate(Guid lobbyId, Action<Race> mutate);
    Task<T> Read<T>(Guid lobbyId, Func<Race, T> read);
    Task<T?> TryRead<T>(Guid lobbyId, Func<Race, T> read);
    void Remove(Guid lobbyId);
    bool Exists(Guid lobbyId);
}
