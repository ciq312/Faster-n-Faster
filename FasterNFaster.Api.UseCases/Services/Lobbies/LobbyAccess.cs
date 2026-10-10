using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.Core.Exceptions.Lobbies;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyAccess(
    ILobbyRepository repo,
    IPlayerLocationRegistry locationRegistry,
    IEventDispatcher dispatcher,
    ILobbyStateTracker tracker) : ILobbyAccess
{
    private readonly AggregateGate<Lobby> gate = new(dispatcher, repo.GetRequired);

    public async Task Mutate(Guid lobbyId, Action<Lobby> mutate)
    {
        await gate.Mutate(lobbyId, lobby =>
        {
            var before = lobby.GetPlayerIds().ToHashSet();
            mutate(lobby);
            repo.Update(lobby);
            SyncLocations(lobbyId, before, lobby);
        });

        tracker.MarkChanged(lobbyId);
    }

    public async Task<Lobby> Create(string lobbyName, bool isPrivate, Guid creatorId)
    {
        if (locationRegistry.GetLobbyIdOfPlayer(creatorId) != null)
            throw new AlreadyInLobbyException();

        Lobby lobby = new(lobbyName, isPrivate, code => repo.GetByInviteCode(code) != null);
        lobby.AssignHost(creatorId);

        repo.Add(lobby);
        await gate.DispatchOf(lobby);
        return lobby;
    }

    public async Task<bool> RemoveIfEmpty(Guid lobbyId)
    {
        var removed = await gate.TryRead(lobbyId, repo.Get, lobby =>
        {
            if (!lobby.IsEmpty()) return false;
            repo.Remove(lobbyId);
            return true;
        });
        if (removed) gate.Release(lobbyId);
        return removed;
    }

    public Task<T> Read<T>(Guid lobbyId, Func<Lobby, T> read) => gate.Read(lobbyId, read);

    public Task<T?> TryRead<T>(Guid lobbyId, Func<Lobby, T> read) => gate.TryRead(lobbyId, repo.Get, read);

    public IReadOnlyCollection<Guid> GetAllIds() => [.. repo.GetAll().Select(l => l.Id)];

    public Guid? FindIdByInviteCode(string code) => repo.GetByInviteCode(code)?.Id;

    public Guid? GetLobbyIdOfPlayer(Guid userId) => locationRegistry.GetLobbyIdOfPlayer(userId);

    public Guid GetLobbyIdOfPlayerRequired(Guid userId) => locationRegistry.GetLobbyIdOfPlayerRequired(userId);

    public bool Exists(Guid lobbyId) => repo.Get(lobbyId) != null;

    private void SyncLocations(Guid lobbyId, HashSet<Guid> before, Lobby lobby)
    {
        var after = lobby.GetPlayerIds().ToHashSet();

        foreach (var added in after.Except(before)) locationRegistry.Track(added, lobbyId);
        foreach (var removed in before.Except(after)) locationRegistry.Untrack(removed);
    }
}
