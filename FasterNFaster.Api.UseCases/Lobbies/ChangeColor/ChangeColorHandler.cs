using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.ChangeColor;

public class ChangeColorHandler(ILobbyAccess lobbies, IBroadcaster broadcaster, ILobbyServiceFacade facade) : IRequestHandler<ChangeColorCommand>
{
    public async Task Handle(ChangeColorCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.UserId);
        await lobbies.Mutate(lobbyId, l => l.ChangePlayerColor(command.UserId, command.Color));
        await broadcaster.Broadcast(Audience.Lobby(lobbyId), GameEvents.LobbyState, await facade.GetLobbyStateDTO(lobbyId));
    }
}
