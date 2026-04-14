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

    public Lobby GetRequired(Guid id) => Get(id)
        ?? throw new KeyNotFoundException($"Lobby {id} not found.");

    public Lobby? GetByInviteCode(string code) =>
        _lobbies.FirstOrDefault(l =>
            l.LobbySettings.InviteCode != null &&
            l.LobbySettings.InviteCode.Equals(code, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyCollection<Lobby> GetAll() => _lobbies.AsReadOnly();
}
