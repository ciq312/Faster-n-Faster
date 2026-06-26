using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.StartRace;

public class StartRaceHandler(ILobbySessionService lobbySessionService) : IRequestHandler<StartRaceCommand>
{
    public async Task Handle(StartRaceCommand command, CancellationToken cancellationToken)
    {
        await lobbySessionService.StartSession(command.UserId);
    }
}
