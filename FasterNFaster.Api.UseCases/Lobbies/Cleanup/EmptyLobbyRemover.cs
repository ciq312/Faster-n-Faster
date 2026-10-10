using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;

namespace FasterNFaster.Api.UseCases.Lobbies.Cleanup;

public class EmptyLobbyRemover(
    ILobbyAccess lobbies,
    IRaceAccess races,
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
