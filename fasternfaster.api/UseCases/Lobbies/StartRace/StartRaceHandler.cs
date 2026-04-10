using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.StartRace;

public class StartRaceHandler(ILobbyStore lobbyStore, IRaceTickRegistry raceTickRegistry) : IHandler<StartRaceCommand>
{
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly IRaceTickRegistry raceTickRegistry = raceTickRegistry;

    public Task Handle(StartRaceCommand command)
    {
        var lobby = lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        if (lobby.IsSessionActive) throw new InvalidOperationException("Can't start active lobby");

        lobby.StartSession();

        raceTickRegistry.RegisterLobby(lobby.Id);

        return Task.CompletedTask;
    }
}
