using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.UseCases.Lobbies.Disconnect;

public class DisconnectHandler(
    ILobbySessionService lobbySessionService
    ) : IHandler<DisconnectCommand>
{

    public async Task Handle(DisconnectCommand command)
    {
        await lobbySessionService.RemovePlayerFromLobby(command.PlayerId);
    }
}
