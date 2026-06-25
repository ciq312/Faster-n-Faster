using FasterNFaster.Api.UseCases.Interfaces.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;

public class UpdateProgressHandler(IRaceService raceService) : IRequestHandler<UpdateProgressCommand>
{
    public async Task Handle(UpdateProgressCommand command, CancellationToken cancellationToken)
    {
        await raceService.ProcessUpdate(command.LobbyId, command.UserId, command.Index, command.Mistakes, command.Typed);
    }
}
