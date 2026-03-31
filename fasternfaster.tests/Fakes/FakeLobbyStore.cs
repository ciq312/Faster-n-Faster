using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Interfaces;

namespace FasterNFaster.Tests.Fakes;

public class FakeLobbyStore : ILobbyStore
{
    private readonly List<Lobby> _lobbies = new();

    public void Seed(Lobby lobby) => _lobbies.Add(lobby);

    public void Add(Lobby lobby) => _lobbies.Add(lobby);

    public void Remove(Guid id) => _lobbies.RemoveAll(l => l.Id == id);

    public Lobby? Get(Guid id) => _lobbies.FirstOrDefault(l => l.Id == id);

    public IReadOnlyCollection<Lobby> GetAll() => _lobbies.AsReadOnly();
}
