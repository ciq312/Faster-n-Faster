using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public class TransferHostHandler(ILobbyStore lobbyStore) : IHandler<TransferHostCommand>
{
    private readonly ILobbyStore lobbyStore = lobbyStore;

    public Task Handle(TransferHostCommand command)
    {
        var lobby = lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        lobby.TransferHost(command.UserId, command.TargetPlayerId);

        return Task.CompletedTask;
    }
}
