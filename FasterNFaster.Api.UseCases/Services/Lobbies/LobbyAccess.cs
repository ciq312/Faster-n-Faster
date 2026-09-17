using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.Core.Exceptions.Lobbies;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyAccess(
    ILobbyRepository repo,
    IPlayerLocationRegistry locationRegistry) : ILobbyAccess
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> gates = new();

    public async Task Mutate(Guid lobbyId, Action<Lobby> mutate)
    {
        var gate = gates.GetOrAdd(lobbyId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            var lobby = repo.GetRequired(lobbyId);

            var before = lobby.Players.Select(p => p.Id).ToHashSet();
            mutate(lobby);
            repo.Update(lobby);
            await repo.SaveChanges();
            var after = lobby.Players.Select(p => p.Id).ToHashSet();

            foreach (var added in after.Except(before)) locationRegistry.Track(added, lobbyId);
            foreach (var removed in before.Except(after)) locationRegistry.Untrack(removed);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<Lobby> Create(string lobbyName, bool isPrivate, Guid creatorId)
    {
        if (locationRegistry.GetLobbyIdOfPlayer(creatorId) != null)
            throw new AlreadyInLobbyException();

        Lobby lobby = new(lobbyName, isPrivate);
        lobby.AssignHost(creatorId);
        lobby.GenerateUniqueInviteCode(c => repo.GetByInviteCode(c) != null);

        repo.Add(lobby);
        await repo.SaveChanges();
        return lobby;
    }

    // The gate is deliberately not disposed: concurrent Mutate callers may still
    // Wait/Release on it. SemaphoreSlim owns no OS handle here, so GC collects it;
    // disposing would fault or hang those callers.
    public async Task Remove(Guid lobbyId)
    {
        repo.Remove(lobbyId);
        gates.TryRemove(lobbyId, out _);
        await repo.SaveChanges();
    }

    public Lobby GetRequired(Guid lobbyId) => repo.Get(lobbyId) ?? throw new LobbyNotFoundException(lobbyId);

    public Lobby GetOfPlayerRequired(Guid userId) =>
        GetRequired(locationRegistry.GetLobbyIdOfPlayerRequired(userId));

    public Guid? GetLobbyIdOfPlayer(Guid userId) => locationRegistry.GetLobbyIdOfPlayer(userId);

    public Guid GetLobbyIdOfPlayerRequired(Guid userId) => locationRegistry.GetLobbyIdOfPlayerRequired(userId);

    public bool Exists(Guid lobbyId) => repo.Get(lobbyId) != null;
}
