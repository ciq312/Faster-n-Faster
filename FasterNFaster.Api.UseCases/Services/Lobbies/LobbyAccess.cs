using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.Core.Exceptions.Lobbies;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyAccess(
    ILobbyRepository repo,
    IPlayerLocationRegistry locationRegistry,
    IEventDispatcher dispatcher) : ILobbyAccess
{
    private readonly AggregateGate<Lobby> gate = new(dispatcher, repo.GetRequired);

    public Task Mutate(Guid lobbyId, Action<Lobby> mutate) =>
        gate.Mutate(lobbyId, lobby =>
        {
            var before = lobby.Players.Select(p => p.Id).ToHashSet();
            mutate(lobby);
            repo.Update(lobby);
            SyncLocations(lobbyId, before, lobby);
        });

    public async Task<Lobby> Create(string lobbyName, bool isPrivate, Guid creatorId)
    {
        if (locationRegistry.GetLobbyIdOfPlayer(creatorId) != null)
            throw new AlreadyInLobbyException();

        Lobby lobby = new(lobbyName, isPrivate);
        lobby.AssignHost(creatorId);
        lobby.GenerateUniqueInviteCode(c => repo.GetByInviteCode(c) != null);

        repo.Add(lobby);
        await gate.DispatchOf(lobby);
        return lobby;
    }

    public async Task Remove(Guid lobbyId)
    {
        var lobby = repo.GetRequired(lobbyId);

        repo.Remove(lobbyId);
        gate.Release(lobbyId);

        await gate.DispatchOf(lobby);
    }

    public Lobby GetRequired(Guid lobbyId) => repo.Get(lobbyId) ?? throw new LobbyNotFoundException(lobbyId);

    public Lobby GetOfPlayerRequired(Guid userId) =>
        GetRequired(locationRegistry.GetLobbyIdOfPlayerRequired(userId));

    public Guid? GetLobbyIdOfPlayer(Guid userId) => locationRegistry.GetLobbyIdOfPlayer(userId);

    public Guid GetLobbyIdOfPlayerRequired(Guid userId) => locationRegistry.GetLobbyIdOfPlayerRequired(userId);

    public bool Exists(Guid lobbyId) => repo.Get(lobbyId) != null;

    private void SyncLocations(Guid lobbyId, HashSet<Guid> before, Lobby lobby)
    {
        var after = lobby.Players.Select(p => p.Id).ToHashSet();

        foreach (var added in after.Except(before)) locationRegistry.Track(added, lobbyId);
        foreach (var removed in before.Except(after)) locationRegistry.Untrack(removed);
    }
}
