using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.Infrastructure.Lobbies;

public class InMemoryLobbyStore : ILobbyStore
{
    private readonly ConcurrentDictionary<Guid, Lobby> lobbies = new();

    public void Add(Lobby lobby)
    {
        if (!lobbies.TryAdd(lobby.Id, lobby))
            throw new InvalidOperationException("Failed to add lobby");
    }

    public void Remove(Guid id)
    {
        if (!lobbies.TryRemove(id, out _))
            throw new InvalidOperationException("Failed to remove lobby");
    }

    public Lobby? Get(Guid id) => lobbies.GetValueOrDefault(id);

    public Lobby GetRequired(Guid id) =>
        lobbies.GetValueOrDefault(id) ?? throw new LobbyNotFoundException(id);

    public Lobby? GetByInviteCode(string code) =>
        lobbies.Values.FirstOrDefault(l =>
            l.LobbySettings.InviteCode != null &&
            l.LobbySettings.InviteCode.Equals(code, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyCollection<Lobby> GetAll() => lobbies.Values.ToList();
}
