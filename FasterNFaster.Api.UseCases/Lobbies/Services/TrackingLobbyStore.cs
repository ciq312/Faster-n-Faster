using FasterNFaster.Api.Domain.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.Ports;

namespace FasterNFaster.Api.UseCases.Lobbies.Services;

public sealed class TrackingLobbyStore(ILobbyStore inner, ILobbyStateTracker tracker) : ILobbyStore
{
    public Task<(Guid Id, string InviteCode)> Create(string name, bool isPrivate, Guid hostId) =>
        inner.Create(name, isPrivate, hostId);

    public async Task Mutate(Guid lobbyId, Action<Lobby> mutate)
    {
        await inner.Mutate(lobbyId, mutate);
        tracker.MarkChanged(lobbyId);
    }

    public async Task<bool> TryMutate(Guid lobbyId, Action<Lobby> mutate)
    {
        var mutated = await inner.TryMutate(lobbyId, mutate);
        if (mutated) tracker.MarkChanged(lobbyId);

        return mutated;
    }

    public Task<T> Read<T>(Guid lobbyId, Func<Lobby, T> read) => inner.Read(lobbyId, read);

    public Task<T?> TryRead<T>(Guid lobbyId, Func<Lobby, T> read) => inner.TryRead(lobbyId, read);

    public Task<bool> RemoveIfEmpty(Guid lobbyId) => inner.RemoveIfEmpty(lobbyId);

    public bool Exists(Guid lobbyId) => inner.Exists(lobbyId);

    public IReadOnlyCollection<Guid> GetAllIds() => inner.GetAllIds();

    public Guid? FindIdByInviteCode(string code) => inner.FindIdByInviteCode(code);

    public Guid? GetLobbyIdOfPlayer(Guid playerId) => inner.GetLobbyIdOfPlayer(playerId);

    public Guid GetLobbyIdOfPlayerRequired(Guid playerId) => inner.GetLobbyIdOfPlayerRequired(playerId);

    public bool TryClaimPlayer(Guid playerId, Guid lobbyId) => inner.TryClaimPlayer(playerId, lobbyId);
}
