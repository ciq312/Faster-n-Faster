using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public class TransferHostHandler(ILobbyStore lobbyStore, IEventDispatcher eventDispatcher) : IHandler<TransferHostCommand>
{
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly IEventDispatcher eventDispatcher = eventDispatcher;

    public async Task Handle(TransferHostCommand command)
    {
        var lobby = lobbyStore.GetRequired(command.LobbyId);

        lobby.TransferHost(command.UserId, command.TargetPlayerId);

        User targetHost = lobby.Players.FirstOrDefault(p => command.TargetPlayerId == p.User.Id)?.User
            ?? throw new UserNotFoundException(command.TargetPlayerId);

        await eventDispatcher.Dispatch(new HostChangedEvent(lobby.Id, targetHost.Id, targetHost.Nick));
    }
}
