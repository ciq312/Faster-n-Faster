using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.StartRace;

public class StartRaceHandler(ILobbyServiceFacade lobbySessionService, ILobbyService lobbyService) : IRequestHandler<StartRaceCommand, Guid>
{
    public async Task<Guid> Handle(StartRaceCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbyService.GetLobbyIdOfPlayerRequired(command.UserId);
        await lobbySessionService.StartSession(command.UserId);
        return lobbyId;
    }
}
