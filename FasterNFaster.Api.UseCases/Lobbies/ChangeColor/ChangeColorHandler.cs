using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.ChangeColor;

public class ChangeColorHandler(ILobbyService lobbyService, IBroadcaster broadcaster, ILobbyServiceFacade facade) : IRequestHandler<ChangeColorCommand>
{
    public async Task Handle(ChangeColorCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbyService.GetLobbyIdOfPlayerRequired(command.UserId);
        await lobbyService.ChangePlayerColor(lobbyId, command.UserId, command.Color);
        await broadcaster.Broadcast(Audience.Lobby(lobbyId), GameEvents.LobbyState, await facade.GetLobbyStateDTO(lobbyId));
    }
}
