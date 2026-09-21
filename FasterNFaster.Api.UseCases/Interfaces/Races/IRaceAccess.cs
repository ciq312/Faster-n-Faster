using FasterNFaster.Api.Core.Entities.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IRaceAccess
{
    Task Mutate(Guid lobbyId, Action<Race> mutate);
    Task ProcessUpdate(Guid lobbyId, Guid playerId, int index, int mistakes, string typed);
    Task RefreshPassage(Guid lobbyId);

    Task<List<ParticipantSnapshot>> GetSnapshot(Guid lobbyId);
    Task<IRaceSettings> GetRaceSettings(Guid lobbyId);
    Task<IRaceSettings?> GetRaceSettingsOrDefault(Guid lobbyId);

    void Register(Race race);
    void Remove(Guid lobbyId);
}
