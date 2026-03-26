using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public class TransferHostHandler : IHandler<TransferHostCommand>
{
    private readonly ILobbyStore _lobbyStore;

    public TransferHostHandler(ILobbyStore lobbyStore)
    {
        _lobbyStore = lobbyStore;
    }

    public Task Handle(TransferHostCommand command)
    {
        var lobby = _lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        lobby.TransferHost(command.UserId, command.TargetPlayerId);

        return Task.CompletedTask;
    }
}
