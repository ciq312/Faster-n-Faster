using System.Diagnostics.Eventing.Reader;
using FastEndpoints;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Core.Lobbies.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using Org.BouncyCastle.Bcpg;

namespace FasterNFaster.Api.UseCases.Services;

public class LobbyService(ILobbyStore lobbyStore, IEventDispatcher eventDispatcher) : ILobbyService
{
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly IEventDispatcher eventDispatcher = eventDispatcher;

    public async Task JoinLobby(User user, Guid lobbyId, string? code)
    {
        Lobby lobby = lobbyStore.GetRequired(lobbyId);

        if (lobby.IsPlayerInLobby(user.Id)) return;
        if (await IsPlayerInLobby(user.Id)) throw new InvalidOperationException("Can't join when in lobby");

        await lobbyStore.Mutate(lobbyId, (lobbyToMutate) => lobbyToMutate.Join(user, code));

#if DEBUG
        Log.Information($"Player {user.Id} joined lobby {lobby.Id}");
#endif
    }



    public async Task<Lobby> CreateLobby(string LobbyName, bool isPrivate, WordRace race, Guid creatorId)
    {
        if (await IsPlayerInLobby(creatorId)) throw new InvalidOperationException("Can't create a lobby while in lobby");

        Lobby lobby = new(LobbyName, isPrivate, race);
        lobby.AssignHost(creatorId);
        await lobby.GenerateUniqueInviteCode((c) => lobbyStore.GetByInviteCode(c) != null);

        lobbyStore.Add(lobby);

#if DEBUG
        Log.Information($"Player {creatorId} created lobby {lobby.Id}");
#endif
        return lobby;
    }

    public async Task TransferHost(Guid hostId, Guid userId)
    {
        Lobby lobby = await FindLobbyPlayerIn(userId);

        lobby.TransferHost(hostId, userId);

        await DispatchLobbyEvents(lobby);
    }
    public async Task KickPlayer(Guid hostId, Guid userId)
    {
        Lobby lobby = await FindLobbyPlayerIn(userId);
        lobby.ValidateHost(hostId);
        var kicked = lobby.Players.FirstOrDefault(p => p.User.Id == userId) ?? throw new UserNotFoundException(userId);

        await lobbyStore.Mutate(lobby.Id, (lobbyToMutate) => lobbyToMutate.RemovePlayer(userId));
        await eventDispatcher.Dispatch(new PlayerKickedEvent(kicked.User.Id, lobby.Id, kicked.User.Nick));
        await DispatchLobbyEvents(lobby);

#if DEBUG
        Log.Information($"Player {userId} was kicked from lobby {lobby.Id}");
#endif
    }

    public async Task RemoveLobbyIfEmpty(Guid lobbyId)
    {
        var lobby = lobbyStore.GetRequired(lobbyId);
        if (lobby.Players.Count == 0) lobbyStore.Remove(lobbyId);
    }
    public async Task RemoveFromLobby(Guid userId)
    {
        Lobby lobby = await FindLobbyPlayerIn(userId);
        var removed = lobby.Players.FirstOrDefault(p => p.User.Id == userId) ?? throw new UserNotFoundException(userId);

        await lobbyStore.Mutate(lobby.Id, (lobbyToMutate) => lobbyToMutate.RemovePlayer(userId));
        await eventDispatcher.Dispatch(new PlayerDisconnectedEvent(userId, lobby.Id, removed.User.Nick));
        await DispatchLobbyEvents(lobby);

#if DEBUG
        Log.Information($"Player {userId} was removed form lobby {lobby.Id}");
#endif
    }
    private async Task<bool> IsPlayerInLobby(Guid userId) => await lobbyStore.GetLobbyPlayerIn(userId) != default;

    private async Task<Lobby> FindLobbyPlayerIn(Guid userId)
    {
        var lobbyId = await lobbyStore.GetLobbyPlayerIn(userId);
        if (lobbyId == default) throw new UserNotFoundException(userId, $"lobby {lobbyId}");

        return lobbyStore.GetRequired(lobbyId);
    }
    private async Task DispatchLobbyEvents(Lobby lobby)
    {
        foreach (var domainEvent in lobby.DomainEvents) await domainEvent.Dispatch(eventDispatcher);
        lobby.ClearEvents();
    }
}
