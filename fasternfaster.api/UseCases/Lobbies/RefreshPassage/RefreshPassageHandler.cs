using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;

public class RefreshPassageHandler(
    ILobbyStore store,
    ILobbySessionService lobbySessionService,
    ILobbyStateBroadcaster broadcaster) : IRequestHandler<RefreshPassageCommand>
{
    public async Task Handle(RefreshPassageCommand command, CancellationToken cancellationToken)
    {
        var lobby = store.Get(command.LobbyId) ?? throw new LobbyNotFoundException(command.LobbyId);
        await lobbySessionService.RefreshPassage(command.CallerId);
        await broadcaster.BroadcastLobbyState(lobby);
    }
}
