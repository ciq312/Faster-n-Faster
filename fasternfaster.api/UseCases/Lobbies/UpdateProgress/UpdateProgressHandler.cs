using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;

public class UpdateProgressHandler(IRaceService raceService) : IHandler<UpdateProgressCommand>
{

    public async Task Handle(UpdateProgressCommand command)
    {
        await raceService.ProcessUpdate(command.LobbyId,
         command.UserId, command.Index, command.Mistakes, 
         command.Typed);
    }
}
