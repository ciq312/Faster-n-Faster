using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyService
{
    Task JoinLobby(User user, Guid lobbyId, string? code);
    Task RemoveFromLobby(Guid userId);
    Task KickPlayer(Guid hostId, Guid userId);
    Task RemoveLobbyIfEmpty(Guid lobbyId);
    Task TransferHost(Guid hostId, Guid userId);
    Task<Lobby> CreateLobby(string LobbyName, bool isPrivate, WordRace race, Guid creatorId);

    Task StartRace(Guid lobbyId);
    Task LaunchSession(Guid lobbyId);
    Task ChangePlayerColor(Guid lobbyId, Guid userId, string color);
    Task SetRacePassage(Guid lobbyId, Guid hostId, string passage);

    Guid? GetLobbyOfPlayer(Guid userId);
}
