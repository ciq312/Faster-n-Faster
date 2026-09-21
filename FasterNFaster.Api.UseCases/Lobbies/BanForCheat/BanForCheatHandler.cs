using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.BanForCheat;

public class BanForCheatHandler(IBanRepository banService, ISender sender) : IRequestHandler<BanForCheatCommand>
{
    public async Task Handle(BanForCheatCommand command, CancellationToken cancellationToken)
    {
        await banService.BanAsync(command.UserId, command.Reason);
        await sender.Send(new DisconnectCommand(command.UserId), cancellationToken);
    }
}
