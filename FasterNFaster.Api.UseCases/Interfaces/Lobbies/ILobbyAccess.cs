using FasterNFaster.Api.Core.Entities.Lobbies;

namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyAccess
{
    Task Mutate(Guid lobbyId, Action<Lobby> mutate);

    Task<Lobby> Create(string lobbyName, bool isPrivate, Guid creatorId);
    Task Remove(Guid lobbyId);

    Lobby GetRequired(Guid lobbyId);
    Lobby GetOfPlayerRequired(Guid userId);
    Guid? GetLobbyIdOfPlayer(Guid userId);
    Guid GetLobbyIdOfPlayerRequired(Guid userId);
    bool Exists(Guid lobbyId);
}
