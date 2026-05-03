using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.UseCases.Lobbies.StartRace;

public class StartRaceHandler(ILobbyService lobbyService, IRaceTickRegistry raceTickRegistry) : IHandler<StartRaceCommand>
{
    private readonly ILobbyService lobbyService = lobbyService;
    private readonly IRaceTickRegistry raceTickRegistry = raceTickRegistry;

    public async Task Handle(StartRaceCommand command)
    {
        await lobbyService.StartRace(command.LobbyId);
        raceTickRegistry.RegisterLobby(command.LobbyId);
    }
}
