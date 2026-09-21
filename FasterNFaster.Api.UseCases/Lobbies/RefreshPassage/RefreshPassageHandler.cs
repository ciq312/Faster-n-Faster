using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;

public class RefreshPassageHandler(
    ILobbyService lobbyService,
    ILobbyServiceFacade facade,
    IBroadcaster broadcaster) : IRequestHandler<RefreshPassageCommand>
{
    public async Task Handle(RefreshPassageCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbyService.GetLobbyIdOfPlayerRequired(command.CallerId);
        await facade.RefreshPassage(command.CallerId);
        await broadcaster.Broadcast(Audience.Lobby(lobbyId), GameEvents.LobbyState, await facade.GetLobbyStateDTO(lobbyId));
    }
}
