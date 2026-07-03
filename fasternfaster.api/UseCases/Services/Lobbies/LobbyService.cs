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
using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyService(
    ILobbyStore lobbyStore,
    IEventDispatcher eventDispatcher,
    IPlayerLocationRegistry locationRegistry) : ILobbyService, ILobbyInternals
{
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly IEventDispatcher eventDispatcher = eventDispatcher;
    private readonly IPlayerLocationRegistry locationRegistry = locationRegistry;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> gates = new();

    public async Task JoinLobby(User user, Guid lobbyId, string? code)
    {
        if (locationRegistry.GetLobbyIdOfPlayer(user.Id) is Guid existingId && existingId != lobbyId)
            throw new AlreadyInLobbyException();

        List<IDomainEvent> events = [];
        await WithLobby(lobbyId, lobby =>
        {
            lobby.Join(user, code);
            events = [.. lobby.DomainEvents];
            lobby.ClearEvents();
        });
        await DispatchEvents(events);
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

        List<IDomainEvent> events = [];
        await WithLobby(lobbyId, l =>
        {
            l.TransferHost(hostId, userId);
            events = [.. l.DomainEvents];
            l.ClearEvents();
        });
        await DispatchEvents(events);
    }

    public async Task KickPlayer(Guid hostId, Guid userId)
    {
        var lobbyId = locationRegistry.GetLobbyIdOfPlayerRequired(userId);

        LobbyPlayer kicked = null!;
        List<IDomainEvent> events = [];
        await WithLobby(lobbyId, l =>
        {
            if (l.IsSessionActive) throw new ConflictException("Can't kick when racing");

            l.ValidateHost(hostId);
            kicked = l.RemovePlayer(userId);
            l.BanPlayer(kicked.User.Id);
            events = [.. l.DomainEvents];
            l.ClearEvents();
        });

        await eventDispatcher.Dispatch(new PlayerKickedEvent(userId, lobbyId, kicked.User.Nick), CancellationToken.None);
        await DispatchEvents(events);
    }

    public async Task RemoveFromLobby(Guid userId)
    {
        var lobbyId = locationRegistry.GetLobbyIdOfPlayerRequired(userId);

        LobbyPlayer removed = null!;
        List<IDomainEvent> events = [];
        await WithLobby(lobbyId, l =>
        {
            removed = l.RemovePlayer(userId);
            events = [.. l.DomainEvents];
            l.ClearEvents();
        });

        await eventDispatcher.Dispatch(new PlayerDisconnectedEvent(userId, lobbyId, removed.User.Nick), CancellationToken.None);
        await DispatchEvents(events);
    }

    public async Task StartSession(Guid lobbyId, Guid hostId)
    {
        List<IDomainEvent> events = [];
        await WithLobby(lobbyId, lobby =>
        {
            lobby.ValidateHost(hostId);
            lobby.StartSession();
            events = [.. lobby.DomainEvents];
            lobby.ClearEvents();
        });
        await DispatchEvents(events);
    }

    public Task EndSession(Guid lobbyId) =>
        WithLobby(lobbyId, lobby => lobby.EndSession());

    // The gate is deliberately not disposed: concurrent WithLobby callers may still
    // Wait/Release on it. SemaphoreSlim owns no OS handle here, so GC collects it;
    // disposing would fault or hang those callers.
    public Task RemoveLobby(Guid lobbyId)
    {
        lobbyStore.Remove(lobbyId);
        gates.TryRemove(lobbyId, out _);
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

    private async Task DispatchEvents(List<IDomainEvent> events)
    {
        foreach (var domainEvent in events)
            await eventDispatcher.Dispatch(domainEvent, CancellationToken.None);
    }
}
