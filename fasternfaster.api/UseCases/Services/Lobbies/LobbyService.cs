using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyService(ILobbyStore lobbyStore) : ILobbyService
{
    private ConcurrentDictionary<Guid, Guid> playersInLobbies = new();
    private readonly ILobbyStore lobbyStore = lobbyStore;

    public async Task JoinLobby(User user, Guid lobbyId)
    {
        Lobby lobby = lobbyStore.GetRequired(lobbyId);

        if (lobby.IsPlayerInLobby(user.Id)) return;
        if (await IsPlayerInLobby(user.Id)) throw new InvalidOperationException("Can't join when in lobby");

        lobby.AddPlayer(user);

        playersInLobbies[user.Id] = lobbyId;
    }

    public async Task<Lobby> CreateLobby(string LobbyName, bool isPrivate, WordRace race, User creator)
    {
        if (await IsPlayerInLobby(creator.Id)) throw new InvalidOperationException("Can't create a lobby while in lobby");

        Lobby lobby = new(LobbyName, isPrivate, race);
        lobby.AssignHost(creator.Id);

        await lobby.GenerateUniqueInviteCode((c) => lobbyStore.GetByInviteCode(c) != null);

        lobbyStore.Add(lobby);

        return lobby;
    }

    public Task KickPlayer(Guid hostId, Guid userId)
    {
        var lobbyId = playersInLobbies.GetValueOrDefault(userId);

        if (lobbyId == default) throw new InvalidOperationException($"Player {userId} not found in lobby");

        Lobby lobby = lobbyStore.GetRequired(lobbyId);

        lobby.KickPlayer(hostId, userId);

        playersInLobbies.Remove(userId, out _);

        return Task.CompletedTask;
    }

    public Task RemovePlayer(Guid userId)
    {
        var lobbyId = playersInLobbies.GetValueOrDefault(userId);

        if (lobbyId == default) throw new InvalidOperationException($"Player {userId} not found in lobby");

        Lobby lobby = lobbyStore.GetRequired(lobbyId);

        lobby.RemovePlayer(userId);

        playersInLobbies.Remove(userId, out _);

        return Task.CompletedTask;
    }
    private Task<bool> IsPlayerInLobby(Guid userId) => Task.FromResult(playersInLobbies.ContainsKey(userId));
}
