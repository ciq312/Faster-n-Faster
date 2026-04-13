using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.Web.Lobbies.LobbyState;

namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public class TransferHostHandler(ILobbyStore lobbyStore, LobbyStateBroadcaster broadcaster) : IHandler<TransferHostCommand, TransferHostResult>
{
    private readonly ILobbyStore lobbyStore = lobbyStore;

    private readonly LobbyStateBroadcaster broadcaster = broadcaster;

    async Task<TransferHostResult> Handle(TransferHostCommand command)
    {
        var lobby = lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        lobby.TransferHost(command.UserId, command.TargetPlayerId);

        await broadcaster.BroadcastLobbyState(lobby);

        User targetHost = lobby.Players.FirstOrDefault(p => command.TargetPlayerId == p.User.Id)?.User ?? throw new UserNotFoundException(command.TargetPlayerId);

        return new TransferHostResult(targetHost.Nick);
    }

    Task<TransferHostResult> IHandler<TransferHostCommand, TransferHostResult>.Handle(TransferHostCommand command)
    {
        return Handle(command);
    }
}

public record TransferHostResult(string Nick);
