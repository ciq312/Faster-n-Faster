using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.BanForCheat;

public class BanForCheatHandler(IBanService banService, ILobbySessionService lobbySessionService) : IRequestHandler<BanForCheatCommand>
{
    public async Task Handle(BanForCheatCommand command, CancellationToken cancellationToken)
    {
        await banService.BanAsync(command.UserId, command.Reason);
        await lobbySessionService.RemovePlayerFromLobby(command.UserId);
    }
}
