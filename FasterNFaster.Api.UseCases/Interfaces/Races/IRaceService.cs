using FasterNFaster.Api.Core.Entities.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IRaceService
{
    Task<List<ParticipantSnapshot>> GetSnapshot(Guid lobbyId);

    Task ProcessUpdate(Guid lobbyId, Guid playerId, int index, int mistakes, string typed);

    void RegisterRace(Guid lobbyId, Race race);

    void RemoveRegisteredRace(Guid lobbyId);

    Task<IRaceSettings> GetRaceSettings(Guid lobbyId);

    Task<IRaceSettings?> GetRaceSettingsOrDefault(Guid lobbyId);
}
