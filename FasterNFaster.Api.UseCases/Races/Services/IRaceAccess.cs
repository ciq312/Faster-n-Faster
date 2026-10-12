using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.UseCases.Lobbies.LobbyState;

namespace FasterNFaster.Api.UseCases.Races.Services;

public interface IRaceAccess
{
    Task Mutate(Guid lobbyId, Action<Race> mutate);
    Task<bool> TryMutate(Guid lobbyId, Action<Race> mutate);
    Task ProcessUpdate(Guid lobbyId, Guid playerId, int index, int mistakes, string typed);
    Task RefreshPassage(Guid lobbyId);

    Task<List<ParticipantSnapshot>?> GetSnapshotOrDefault(Guid lobbyId);
    Task<RaceSettingsDTO> GetRaceSettings(Guid lobbyId);
    Task<RaceSettingsDTO?> GetRaceSettingsOrDefault(Guid lobbyId);

    void Register(Race race);
    void Remove(Guid lobbyId);
}
