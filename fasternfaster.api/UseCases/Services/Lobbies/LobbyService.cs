using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Lobbies.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.Core.Exceptions.Lobbies;
using FasterNFaster.Api.Core.Helpers;
using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyService(
    ILobbyStore lobbyStore,
    IAggregateRootHelper aggregateRootHelper,
    IEventDispatcher eventDispatcher,
    IPlayerLocationRegistry locationRegistry) : ILobbyService, ILobbyInternals
{
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly IEventDispatcher eventDispatcher = eventDispatcher;
    private readonly IAggregateRootHelper aggregateRootHelper = aggregateRootHelper;
    private readonly IPlayerLocationRegistry locationRegistry = locationRegistry;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> gates = new();

    public async Task JoinLobby(User user, Guid lobbyId, string? code)
    {
        if (locationRegistry.GetLobbyIdOfPlayer(user.Id) is Guid existingId && existingId != lobbyId)
            throw new AlreadyInLobbyException();

        await WithLobby(lobbyId, lobby => lobby.Join(user, code));
    }

    public ValueTask<Lobby> CreateLobby(string LobbyName, bool isPrivate, Guid creatorId)
    {
        if (locationRegistry.GetLobbyIdOfPlayer(creatorId) != null)
            throw new AlreadyInLobbyException();

        Lobby lobby = new(LobbyName, isPrivate);
        lobby.AssignHost(creatorId);
        lobby.GenerateUniqueInviteCode(c => lobbyStore.GetByInviteCode(c) != null);

        lobbyStore.Add(lobby);
        return ValueTask.FromResult(lobby);
    }

    public async Task TransferHost(Guid hostId, Guid userId)
    {
        var lobbyId = locationRegistry.GetLobbyIdOfPlayerRequired(userId);

        await WithLobby(lobbyId, l => l.TransferHost(hostId, userId));
        await aggregateRootHelper.DispatchRootEventsAsync(lobbyStore.GetRequired(lobbyId));
    }

    public async Task KickPlayer(Guid hostId, Guid userId)
    {
        var lobbyId = locationRegistry.GetLobbyIdOfPlayerRequired(userId);

        LobbyPlayer kicked = null!;
        await WithLobby(lobbyId, l =>
        {
            if (l.IsSessionActive) throw new ConflictException("Can't kick when racing");

            l.ValidateHost(hostId);
            kicked = l.RemovePlayer(userId);
            l.BanPlayer(kicked.User.Id);
        });

        await eventDispatcher.Dispatch((dynamic)new PlayerKickedEvent(userId, lobbyId, kicked.User.Nick), CancellationToken.None);
        await aggregateRootHelper.DispatchRootEventsAsync(lobbyStore.GetRequired(lobbyId));
    }

    public async Task RemoveFromLobby(Guid userId)
    {
        var lobbyId = locationRegistry.GetLobbyIdOfPlayerRequired(userId);

        LobbyPlayer removed = null!;
        await WithLobby(lobbyId, l => removed = l.RemovePlayer(userId));

        await eventDispatcher.Dispatch((dynamic)new PlayerDisconnectedEvent(userId, lobbyId, removed.User.Nick), CancellationToken.None);
        await aggregateRootHelper.DispatchRootEventsAsync(lobbyStore.GetRequired(lobbyId));
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

    public Task RemoveLobby(Guid lobbyId)
    {
        lobbyStore.Remove(lobbyId);
        if (gates.TryRemove(lobbyId, out var sem)) sem.Dispose();
        return Task.CompletedTask;
    }

    public Task ChangePlayerColor(Guid lobbyId, Guid userId, string color) =>
        WithLobby(lobbyId, lobby => lobby.ChangePlayerColor(userId, color));

    public Guid? GetLobbyIdOfPlayer(Guid userId) => locationRegistry.GetLobbyIdOfPlayer(userId);

    public Guid GetLobbyIdOfPlayerRequired(Guid userId) => locationRegistry.GetLobbyIdOfPlayerRequired(userId);

    public Lobby GetLobbyRequired(Guid lobbyId) => lobbyStore.Get(lobbyId) ?? throw new LobbyNotFoundException(lobbyId);

    public Lobby GetLobbyOfPlayerRequired(Guid userId)
    {
        Guid lobbyId = locationRegistry.GetLobbyIdOfPlayerRequired(userId);
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
}
