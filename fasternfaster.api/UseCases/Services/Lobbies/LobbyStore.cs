
using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyStore : ILobbyStore
{
    private ConcurrentDictionary<Guid, Guid> playersInLobbies = new();

    private readonly ConcurrentDictionary<Guid, Lobby> lobbies = new();

    public void Add(Lobby lobby)
    {
        if (!lobbies.TryAdd(lobby.Id, lobby)) throw new InvalidOperationException("Failed to add lobby");
    }

    public void Remove(Guid id)
    {
        if (!lobbies.TryRemove(id, out _)) throw new InvalidOperationException("Failed to remove lobby");

    }
    public Lobby? Get(Guid id) => lobbies.GetValueOrDefault(id);

    public Lobby? GetByInviteCode(string code) =>
        lobbies.Values.FirstOrDefault(l =>
            l.LobbySettings.InviteCode != null &&
            l.LobbySettings.InviteCode.Equals(code, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyCollection<Lobby> GetAll() => lobbies.Values.ToList();

    public Lobby GetRequired(Guid id) => lobbies.GetValueOrDefault(id) ?? throw new LobbyNotFoundException(id);

    public Task Mutate(Guid lobbyId, Action<Lobby> mutate)
    {
        var lobby = GetRequired(lobbyId);

        var before = lobby.Players.Select(p => p.User.Id).ToHashSet();
        mutate(lobby);
        var after = lobby.Players.Select(p => p.User.Id).ToHashSet();

        foreach (var added in after.Except(before)) playersInLobbies[added] = lobbyId;
        foreach (var removed in before.Except(after)) playersInLobbies.Remove(removed, out _);

        return Task.CompletedTask;
    }

    public Task<Guid> GetLobbyPlayerIn(Guid userId) => Task.FromResult(playersInLobbies.GetValueOrDefault(userId));
}
