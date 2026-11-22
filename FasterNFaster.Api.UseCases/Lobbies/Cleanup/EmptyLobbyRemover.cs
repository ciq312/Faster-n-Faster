using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Races.Ports;
using FasterNFaster.Api.UseCases.Shared;

namespace FasterNFaster.Api.UseCases.Lobbies.Cleanup;

public class EmptyLobbyRemover(
    ILobbyStore lobbies,
    IRaceStore races,
    IRaceTickRegistry raceTickRegistry,
    ILogger<EmptyLobbyRemover> logger)
{
    public async Task<bool> TryRemove(Guid lobbyId)
    {
        if (!await lobbies.RemoveIfEmpty(lobbyId)) return false;

        raceTickRegistry.DeregisterLobby(lobbyId);
        races.Remove(lobbyId);

        logger.LogInformation("lobby {LobbyId} was removed {TelemetryOperation}", lobbyId, TelemetryOperations.LobbyRemoved);
        return true;
    }
}
