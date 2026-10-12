using System.Collections.Concurrent;
using FasterNFaster.Api.Domain.Entities.Lobbies;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Domain.Interfaces.Events;
using FasterNFaster.Api.UseCases.Lobbies.Exceptions;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Shared;

namespace FasterNFaster.Api.Infrastructure.Lobbies;

public sealed class InMemoryLobbyStore : ILobbyStore
{
    private readonly ConcurrentDictionary<Guid, Lobby> lobbies = new();
    private readonly ConcurrentDictionary<string, Guid> inviteCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, Guid> playerLobbies = new();
    private readonly AggregateGate<Lobby> gate;
    private readonly Func<Guid, Lobby?> tryGet;

    public InMemoryLobbyStore(IEventDispatcher dispatcher)
    {
        gate = new AggregateGate<Lobby>(dispatcher, GetRequired);
        tryGet = lobbies.GetValueOrDefault;
    }

    public async Task<(Guid Id, string InviteCode)> Create(string name, bool isPrivate, Guid hostId)
    {
        Lobby lobby = new(name, isPrivate, inviteCodes.ContainsKey);
        lobby.AssignHost(hostId);

        var inviteCode = lobby.LobbySettings.InviteCode;
        lobbies[lobby.Id] = lobby;
        inviteCodes[inviteCode] = lobby.Id;
        await gate.DispatchOf(lobby);

        return (lobby.Id, inviteCode);
    }

    public Task Mutate(Guid lobbyId, Action<Lobby> mutate) =>
        gate.Mutate(lobbyId, MutateAndSyncPlayers(lobbyId, mutate));

    public Task<bool> TryMutate(Guid lobbyId, Action<Lobby> mutate) =>
        gate.TryMutate(lobbyId, tryGet, MutateAndSyncPlayers(lobbyId, mutate));

    public Task<T> Read<T>(Guid lobbyId, Func<Lobby, T> read) => gate.Read(lobbyId, read);

    public Task<T?> TryRead<T>(Guid lobbyId, Func<Lobby, T> read) => gate.TryRead(lobbyId, tryGet, read);

    public async Task<bool> RemoveIfEmpty(Guid lobbyId)
    {
        var removed = await gate.TryRead(lobbyId, tryGet, lobby =>
        {
            if (!lobby.IsEmpty()) return false;

            lobbies.TryRemove(lobbyId, out _);
            inviteCodes.TryRemove(new KeyValuePair<string, Guid>(lobby.LobbySettings.InviteCode, lobbyId));
            return true;
        });
        if (removed) gate.Release(lobbyId);
        return removed;
    }

    public bool Exists(Guid lobbyId) => lobbies.ContainsKey(lobbyId);

    public IReadOnlyCollection<Guid> GetAllIds() => [.. lobbies.Keys];

    public Guid? FindIdByInviteCode(string code) =>
        inviteCodes.TryGetValue(code, out var id) ? id : null;

    public Guid? GetLobbyIdOfPlayer(Guid playerId) =>
        playerLobbies.TryGetValue(playerId, out var id) ? id : null;

    public Guid GetLobbyIdOfPlayerRequired(Guid playerId) =>
        GetLobbyIdOfPlayer(playerId) ?? throw new UserNotFoundException(playerId);

    public bool TryClaimPlayer(Guid playerId, Guid lobbyId) =>
        playerLobbies.GetOrAdd(playerId, lobbyId) == lobbyId;

    private Action<Lobby> MutateAndSyncPlayers(Guid lobbyId, Action<Lobby> mutate) => lobby =>
    {
        var before = lobby.GetPlayerIds().ToHashSet();
        mutate(lobby);
        var after = lobby.GetPlayerIds().ToHashSet();

        foreach (var added in after.Except(before)) playerLobbies[added] = lobbyId;
        foreach (var removed in before.Except(after))
            playerLobbies.TryRemove(new KeyValuePair<Guid, Guid>(removed, lobbyId));
    };

    private Lobby GetRequired(Guid lobbyId) =>
        lobbies.GetValueOrDefault(lobbyId) ?? throw new LobbyNotFoundException(lobbyId);
}
