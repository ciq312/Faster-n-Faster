using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;

public class RefreshPassageHandler(
    ILobbyService lobbyService,
    ILobbySessionService lobbySessionService,
    ILobbyStateBroadcaster broadcaster) : IRequestHandler<RefreshPassageCommand>
{
    public async Task Handle(RefreshPassageCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbyService.GetLobbyIdOfPlayerRequired(command.CallerId);
        await lobbySessionService.RefreshPassage(command.CallerId);
        await broadcaster.BroadcastLobbyState(lobbyId);
    }
}
