using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;

public class UpdateProgressHandler(IRaceService raceService, ILobbyService lobbyService) : IRequestHandler<UpdateProgressCommand>
{
    public async Task Handle(UpdateProgressCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbyService.GetLobbyIdOfPlayerRequired(command.UserId);
        // await raceService.ProcessUpdate(lobbyId, command.UserId, command.Index, command.Mistakes, command.Typed);
    }
}
