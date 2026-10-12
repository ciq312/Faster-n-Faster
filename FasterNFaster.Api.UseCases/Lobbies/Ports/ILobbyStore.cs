using FasterNFaster.Api.Domain.Entities.Lobbies;

namespace FasterNFaster.Api.UseCases.Lobbies.Ports;

public interface ILobbyStore
{
    Task<(Guid Id, string InviteCode)> Create(string name, bool isPrivate, Guid hostId);
    Task Mutate(Guid lobbyId, Action<Lobby> mutate);
    Task<bool> TryMutate(Guid lobbyId, Action<Lobby> mutate);
    Task<T> Read<T>(Guid lobbyId, Func<Lobby, T> read);
    Task<T?> TryRead<T>(Guid lobbyId, Func<Lobby, T> read);
    Task<bool> RemoveIfEmpty(Guid lobbyId);
    bool Exists(Guid lobbyId);
    IReadOnlyCollection<Guid> GetAllIds();
    Guid? FindIdByInviteCode(string code);
    Guid? GetLobbyIdOfPlayer(Guid playerId);
    Guid GetLobbyIdOfPlayerRequired(Guid playerId);
}
