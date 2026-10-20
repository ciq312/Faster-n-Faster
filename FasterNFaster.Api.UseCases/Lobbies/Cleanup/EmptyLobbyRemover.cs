using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Races.Ports;

namespace FasterNFaster.Api.UseCases.Lobbies.Cleanup;

public class EmptyLobbyRemover(
    ILobbyStore lobbies,
    IRaceStore races,
    IRaceTickRegistry raceTickRegistry)
{
    public async Task<bool> TryRemove(Guid lobbyId)
    {
        if (!await lobbies.RemoveIfEmpty(lobbyId)) return false;

        raceTickRegistry.DeregisterLobby(lobbyId);
        races.Remove(lobbyId);
        return true;
    }
}
