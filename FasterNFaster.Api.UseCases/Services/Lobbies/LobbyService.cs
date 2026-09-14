using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.Core.Exceptions.Lobbies;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyService(
    ILobbyRepository repo,
    IPlayerLocationRegistry locationRegistry) : ILobbyService, ILobbyInternals
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> gates = new();

    public async Task JoinLobby(User user, Guid lobbyId, string? code)
    {
        if (locationRegistry.GetLobbyIdOfPlayer(user.Id) is Guid existingId && existingId != lobbyId)
            throw new AlreadyInLobbyException();

        await WithLobby(lobbyId, lobby =>
        {
            lobby.Join(user.Id, user.Nick, code);
        });
    }

    public async Task<Lobby> CreateLobby(string LobbyName, bool isPrivate, Guid creatorId)
    {
        if (locationRegistry.GetLobbyIdOfPlayer(creatorId) != null)
            throw new AlreadyInLobbyException();

        Lobby lobby = new(LobbyName, isPrivate);
        lobby.AssignHost(creatorId);
        lobby.GenerateUniqueInviteCode(c => repo.GetByInviteCode(c) != null);

        repo.Add(lobby);
        await repo.SaveChanges();
        return lobby;
    }

    public async Task TransferHost(Guid hostId, Guid userId)
    {
        var lobbyId = locationRegistry.GetLobbyIdOfPlayerRequired(userId);

        await WithLobby(lobbyId, l =>
        {
            l.TransferHost(hostId, userId);
        });
    }

    public async Task KickPlayer(Guid hostId, Guid userId)
    {
        var lobbyId = locationRegistry.GetLobbyIdOfPlayerRequired(userId);

        await WithLobby(lobbyId, l =>
        {
            l.Kick(hostId, userId);
        });

    }

    public async Task RemoveFromLobby(Guid userId)
    {
        var lobbyId = locationRegistry.GetLobbyIdOfPlayerRequired(userId);

        await WithLobby(lobbyId, l => l.Disconnect(userId));
    }

    public async Task StartSession(Guid lobbyId, Guid hostId)
    {
        await WithLobby(lobbyId, lobby =>
        {
            lobby.ValidateHost(hostId);
            lobby.StartSession();
        });
    }

    public Task EndSession(Guid lobbyId) =>
        WithLobby(lobbyId, lobby => lobby.EndSession());

    // The gate is deliberately not disposed: concurrent WithLobby callers may still
    // Wait/Release on it. SemaphoreSlim owns no OS handle here, so GC collects it;
    // disposing would fault or hang those callers.
    public async Task RemoveLobby(Guid lobbyId)
    {
        repo.Remove(lobbyId);
        gates.TryRemove(lobbyId, out _);
        await repo.SaveChanges();
    }

    public Task ChangePlayerColor(Guid lobbyId, Guid userId, string color) =>
        WithLobby(lobbyId, lobby => lobby.ChangePlayerColor(userId, color));

    public Guid? GetLobbyIdOfPlayer(Guid userId) => locationRegistry.GetLobbyIdOfPlayer(userId);

    public Guid GetLobbyIdOfPlayerRequired(Guid userId) => locationRegistry.GetLobbyIdOfPlayerRequired(userId);

    public Lobby GetLobbyRequired(Guid lobbyId) => repo.Get(lobbyId) ?? throw new LobbyNotFoundException(lobbyId);

    public Lobby GetLobbyOfPlayerRequired(Guid userId)
    {
        Guid lobbyId = locationRegistry.GetLobbyIdOfPlayerRequired(userId);
        return repo.Get(lobbyId) ?? throw new LobbyNotFoundException(lobbyId);
    }

    private async Task WithLobby(Guid lobbyId, Action<Lobby> action)
    {
        var sem = gates.GetOrAdd(lobbyId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        try
        {
            var lobby = repo.GetRequired(lobbyId);

            var before = lobby.Players.Select(p => p.Id).ToHashSet();
            action(lobby);
            repo.Update(lobby);
            await repo.SaveChanges();
            var after = lobby.Players.Select(p => p.Id).ToHashSet();

            foreach (var added in after.Except(before)) locationRegistry.Track(added, lobbyId);
            foreach (var removed in before.Except(after)) locationRegistry.Untrack(removed);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task ValidateHost(Guid lobbyId, Guid hostId)
    {
        await WithLobby(lobbyId, l => l.ValidateHost(hostId));
    }

    public Task<bool> DoesLobbyExist(Guid lobbyId)
    {
        return Task.FromResult(repo.Get(lobbyId) != null);
    }
}
