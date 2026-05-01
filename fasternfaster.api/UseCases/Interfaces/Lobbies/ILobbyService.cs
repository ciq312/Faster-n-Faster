using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyService
{
    public Task JoinLobby(User user, Guid lobbyId, string? code);

    public Task RemoveFromLobby(Guid userId);

    public Task KickPlayer(Guid hostId, Guid userId);
    public Task RemoveLobbyIfEmpty(Guid lobbyId);
    public Task TransferHost(Guid hostId, Guid userId);
    public Task<Lobby> CreateLobby(string LobbyName, bool isPrivate, WordRace race, Guid creatorId);
}
