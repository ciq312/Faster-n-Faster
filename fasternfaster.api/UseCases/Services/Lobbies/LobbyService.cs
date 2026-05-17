using System.Collections.Concurrent;
using FastEndpoints;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Core.Lobbies.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.Core.Helpers;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyService(ILobbyStore lobbyStore, IAggregateRootHelper aggregateRootHelper, IEventDispatcher eventDispatcher) : ILobbyService, ILobbyCoordinator
{
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly IEventDispatcher eventDispatcher = eventDispatcher;
    private readonly IAggregateRootHelper aggregateRootHelper = aggregateRootHelper;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> gates = new();

    private readonly ConcurrentDictionary<Guid, Guid> playerToLobby = new();

    public async Task JoinLobby(User user, Guid lobbyId, string? code)
    {
        if (playerToLobby.TryGetValue(user.Id, out var existingLobbyId) && existingLobbyId != lobbyId)
            throw new InvalidOperationException("Can't join when in lobby");

        await WithLobby(lobbyId, lobby => lobby.Join(user, code));

#if DEBUG
        Log.Information($"Player {user.Id} joined lobby {lobbyId}");
#endif
    }

    public async Task<Lobby> CreateLobby(string LobbyName, bool isPrivate, Guid creatorId)
    {
        if (playerToLobby.ContainsKey(creatorId))
            throw new InvalidOperationException("Can't create a lobby while in lobby");

        Lobby lobby = new(LobbyName, isPrivate);
        lobby.AssignHost(creatorId);
        await lobby.GenerateUniqueInviteCode((c) => lobbyStore.GetByInviteCode(c) != null);

        lobbyStore.Add(lobby);

#if DEBUG
        Log.Information($"Player {creatorId} created lobby {lobby.Id}");
#endif
        return lobby;
    }

    public async Task TransferHost(Guid hostId, Guid userId)
    {
        var lobbyId = GetLobbyIdOfPlayerRequired(userId);

        await WithLobby(lobbyId, l => l.TransferHost(hostId, userId));
        aggregateRootHelper.DispatchRootEvents(lobbyStore.GetRequired(lobbyId));
    }

    public async Task KickPlayer(Guid hostId, Guid userId)
    {
        var lobbyId = GetLobbyIdOfPlayerRequired(userId);

        LobbyPlayer kicked = null!;
        await WithLobby(lobbyId, l =>
        {
            if (l.IsSessionActive) throw new DomainException("Can't kick when racing");

            l.ValidateHost(hostId);
            kicked = l.RemovePlayer(userId);
            l.BanPlayer(kicked.User.Id);
        });

        await eventDispatcher.Dispatch(new PlayerKickedEvent(userId, lobbyId, kicked.User.Nick));
        aggregateRootHelper.DispatchRootEvents(lobbyStore.GetRequired(lobbyId));

#if DEBUG
        Log.Information($"Player {userId} was kicked from lobby {lobbyId}");
#endif
    }

    public async Task RemoveFromLobby(Guid userId)
    {
        var lobbyId = GetLobbyIdOfPlayerRequired(userId);

        LobbyPlayer removed = null!;
        await WithLobby(lobbyId, l => removed = l.RemovePlayer(userId));

        await eventDispatcher.Dispatch(new PlayerDisconnectedEvent(userId, lobbyId, removed.User.Nick));
        aggregateRootHelper.DispatchRootEvents(lobbyStore.GetRequired(lobbyId));

#if DEBUG
        Log.Information($"Player {userId} was removed from lobby {lobbyId}");
#endif
    }

    public async Task StartSession(Guid lobbyId, Guid hostId)
    {
        await WithLobby(lobbyId, lobby =>
        {
            lobby.ValidateHost(hostId);
            lobby.StartSession();
        });
    }
    public Task RemoveLobby(Guid lobbyId)
    {
        lobbyStore.Remove(lobbyId);
        if (gates.TryRemove(lobbyId, out var sem)) sem.Dispose();
        return Task.CompletedTask;
    }


    public Task ChangePlayerColor(Guid lobbyId, Guid userId, string color) =>
        WithLobby(lobbyId, lobby => lobby.ChangePlayerColor(userId, color));

    public Guid? GetLobbyIdOfPlayer(Guid userId) =>
        playerToLobby.TryGetValue(userId, out var id) ? id : null;

    public Guid GetLobbyIdOfPlayerRequired(Guid userId) =>
        GetLobbyIdOfPlayer(userId) ?? throw new UserNotFoundException(userId);

    public Lobby GetLobbyRequired(Guid lobbyId) => lobbyStore.Get(lobbyId) ?? throw new LobbyNotFoundException(lobbyId);

    public Lobby GetLobbyOfPlayerRequired(Guid userId)
    {
        Guid lobbyId = GetLobbyIdOfPlayerRequired(userId);
        return lobbyStore.Get(lobbyId) ?? throw new LobbyNotFoundException(lobbyId);
    }
    private async Task WithLobby(Guid lobbyId, Action<Lobby> action)
    {
        var sem = gates.GetOrAdd(lobbyId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        try
        {
            var lobby = lobbyStore.GetRequired(lobbyId);

            var before = lobby.Players.Select(p => p.User.Id).ToHashSet();
            action(lobby);
            var after = lobby.Players.Select(p => p.User.Id).ToHashSet();

            foreach (var added in after.Except(before)) playerToLobby[added] = lobbyId;
            foreach (var removed in before.Except(after)) playerToLobby.TryRemove(removed, out _);
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
}
