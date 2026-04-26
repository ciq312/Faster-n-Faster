using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyService
{
    public Task JoinLobby(User user, Guid lobbyId);

    public Task RemovePlayer(Guid userId);

    public Task KickPlayer(Guid hostId, Guid userId);

    public Task<Lobby> CreateLobby(string LobbyName, bool isPrivate, WordRace race, User creator);
}
