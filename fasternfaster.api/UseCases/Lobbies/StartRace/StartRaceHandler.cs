using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.StartRace;

public class StartRaceHandler : IHandler<StartRaceCommand, StartRaceResult>
{
    private readonly ILobbyStore _lobbyStore;
    private readonly IRaceTickRegistry _raceTickRegistry;

    public StartRaceHandler(ILobbyStore lobbyStore, IRaceTickRegistry raceTickRegistry)
    {
        _lobbyStore = lobbyStore;
        _raceTickRegistry = raceTickRegistry;
    }

    public Task<StartRaceResult> Handle(StartRaceCommand command)
    {
        var lobby = _lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        lobby.StartRace(command.UserId);
        _raceTickRegistry.RegisterLobby(lobby.Id);

        var words = lobby.Race.Words;
        return Task.FromResult(new StartRaceResult(words));
    }
}
