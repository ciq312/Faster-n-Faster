using FasterNFaster.Api.Core.Entities.Lobbies;

namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyAccess
{
    Task Mutate(Guid lobbyId, Action<Lobby> mutate);

    Task<Lobby> Create(string lobbyName, bool isPrivate, Guid creatorId);
    Task<bool> RemoveIfEmpty(Guid lobbyId);

    Task<T> Read<T>(Guid lobbyId, Func<Lobby, T> read);
    Task<T?> TryRead<T>(Guid lobbyId, Func<Lobby, T> read);
    IReadOnlyCollection<Guid> GetAllIds();
    Guid? FindIdByInviteCode(string code);
    Guid? GetLobbyIdOfPlayer(Guid userId);
    Guid GetLobbyIdOfPlayerRequired(Guid userId);
    bool Exists(Guid lobbyId);
}
