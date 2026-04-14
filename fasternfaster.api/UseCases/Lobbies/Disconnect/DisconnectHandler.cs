using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.Disconnect;

public class DisconnectHandler(
    ILobbyStore lobbyStore,
    ILobbyService lobbyService,
    IRaceTickRegistry raceTickRegistry,
    IEventDispatcher eventDispatcher) : IHandler<DisconnectCommand, DisconnectResult>
{
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly ILobbyService lobbyService = lobbyService;
    private readonly IRaceTickRegistry raceTickRegistry = raceTickRegistry;
    private readonly IEventDispatcher eventDispatcher = eventDispatcher;

    public async Task<DisconnectResult> Handle(DisconnectCommand command)
    {
        lobbyService.RemoveConnection(command.ConnectionId);

        var lobby = lobbyStore.GetRequired(command.LobbyId);

        if (!lobby.IsPlayerInLobby(command.PlayerId))
            throw new KeyNotFoundException($"Player not found in lobby {command.LobbyId}.");

        lobby.RemovePlayer(command.PlayerId);

        if (TryDeregisterIfEmpty(lobby))
            return new DisconnectResult(command.PlayerId, ShouldDeregisterTicks: true);

        var newHost = lobby.TryPromoteNextHost(command.PlayerId);
        WithdrawFromActiveRace(lobby, command.PlayerId);

        if (newHost != null)
            await eventDispatcher.Dispatch(
                new HostChangedEvent(lobby.Id, newHost.User.Id, newHost.User.Nick));

        return new DisconnectResult(command.PlayerId, ShouldDeregisterTicks: false);
    }

    private bool TryDeregisterIfEmpty(Lobby lobby)
    {
        if (lobby.Players.Count != 0) return false;

        raceTickRegistry.DeregisterLobby(lobby.Id);
        lobbyStore.Remove(lobby.Id);
        return true;
    }

    private static void WithdrawFromActiveRace(Lobby lobby, Guid playerId)
    {
        if (lobby.IsSessionActive &&
            lobby.Race.Participants.TryGetValue(playerId, out var participant))
            participant.MarkWithdrawn();
    }
}
