using FasterNFaster.Api.Core.Entities.Lobbies.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IRaceService
{
    List<ParticipantSnapshot> GetSnapshot(Guid lobbyId);

    void ProcessUpdate(Guid lobbyId, Guid playerId, int index, int mistakes, string typed);

    void RegisterRace(Guid lobbyId, Race race);

    void RemoveRegisteredRace(Guid lobbyId);

    IRaceSettings GetRaceSettings(Guid lobbyId);
}
