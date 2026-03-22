using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities.Lobby;
using FasterNFaster.Api.Core.Interfaces;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyStore : ILobbyStore
{
    private readonly ConcurrentDictionary<Guid, Lobby> _lobbies = new();

    public void Add(Lobby lobby) => _lobbies[lobby.Id] = lobby;

    public Lobby? Get(Guid id) => _lobbies.GetValueOrDefault(id);

    public IReadOnlyCollection<Lobby> GetAll() => _lobbies.Values.ToList();
}
