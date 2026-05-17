using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.UseCases.Lobbies.StartRace;

public class StartRaceHandler(ILobbySessionService lobbySessionService) : IHandler<StartRaceCommand>
{
    public async Task Handle(StartRaceCommand command)
    {
        await lobbySessionService.StartSession(command.UserId);
    }
}
