
using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Interfaces;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyStore : ILobbyStore
{
    private readonly ConcurrentDictionary<Guid, Lobby> _lobbies = new();

    public void Add(Lobby lobby) => _lobbies[lobby.Id] = lobby;

    public void Remove(Guid id) => _lobbies.TryRemove(id, out _);

    public Lobby? Get(Guid id) => _lobbies.GetValueOrDefault(id);

    public IReadOnlyCollection<Lobby> GetAll() => _lobbies.Values.ToList();
}
