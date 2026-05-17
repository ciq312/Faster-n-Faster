using FasterNFaster.Api.Core.Entities.Lobbies.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IRaceService
{
    Task<List<ParticipantSnapshot>> GetSnapshot(Guid lobbyId);

    Task ProcessUpdate(Guid lobbyId, Guid playerdId, int index, int mistakes, string typed);

    void RegisterRace(Guid lobbyId, Race race);

    void RemoveRegistredRace(Guid lobbyId);

    Type GetRaceType(Guid lobbyId);
    Task<IRaceSettings> GetRaceSettings(Guid lobbyId);
}