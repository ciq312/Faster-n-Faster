using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.Infrastructure.Users;

public class InMemoryPlayerLocationRegistry : IPlayerLocationRegistry
{
    private readonly ConcurrentDictionary<Guid, Guid> playerToLobby = new();

    public Guid? GetLobbyIdOfPlayer(Guid userId) =>
        playerToLobby.TryGetValue(userId, out var id) ? id : null;

    public Guid GetLobbyIdOfPlayerRequired(Guid userId) =>
        GetLobbyIdOfPlayer(userId) ?? throw new UserNotFoundException(userId);

    public void Track(Guid userId, Guid lobbyId) =>
        playerToLobby[userId] = lobbyId;

    public void Untrack(Guid userId) =>
        playerToLobby.TryRemove(userId, out _);
}
