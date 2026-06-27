using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;

namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyService
{
    Task JoinLobby(User user, Guid lobbyId, string? code);
    Task TransferHost(Guid hostId, Guid userId);
    Task<Lobby> CreateLobby(string LobbyName, bool isPrivate, Guid creatorId);
    Task ChangePlayerColor(Guid lobbyId, Guid userId, string color);

    Guid? GetLobbyIdOfPlayer(Guid userId);
    Guid GetLobbyIdOfPlayerRequired(Guid userId);
    Lobby GetLobbyOfPlayerRequired(Guid userId);
    Lobby GetLobbyRequired(Guid lobbyId);
}
