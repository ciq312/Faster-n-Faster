using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.StartRace;

public class StartRaceHandler : IHandler<StartRaceCommand>
{
    private readonly ILobbyStore _lobbyStore;
    private readonly IRaceTickRegistry _raceTickRegistry;

    public StartRaceHandler(ILobbyStore lobbyStore, IRaceTickRegistry raceTickRegistry)
    {
        _lobbyStore = lobbyStore;
        _raceTickRegistry = raceTickRegistry;
    }

    public Task Handle(StartRaceCommand command)
    {
        var lobby = _lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        if (lobby.IsSessionActive) throw new InvalidOperationException("Can't start active lobby");

        lobby.StartSession();

        _raceTickRegistry.RegisterLobby(lobby.Id);

        return Task.CompletedTask;
    }
}
