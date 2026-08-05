using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.BanForCheat;

public class BanForCheatHandler(IBanRepository banService, ILobbyServiceFacade lobbySessionService) : IRequestHandler<BanForCheatCommand>
{
    public async Task Handle(BanForCheatCommand command, CancellationToken cancellationToken)
    {
        await banService.BanAsync(command.UserId, command.Reason);
        await lobbySessionService.RemovePlayerFromLobby(command.UserId);
    }
}
