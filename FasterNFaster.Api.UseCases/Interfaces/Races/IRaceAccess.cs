using FasterNFaster.Api.Core.Entities.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IRaceAccess
{
    // Runs the mutation under the race's gate, then dispatches whatever events it raised
    // once the gate is released, so handlers are free to re-enter the same race.
    // Callers pass a single aggregate call; branching belongs inside Race.
    Task Mutate(Guid lobbyId, Action<Race> mutate);

    // Carry dependencies of their own (anti-cheat policy, passage provider), so they
    // stay named rather than collapsing into Mutate at the call site.
    Task ProcessUpdate(Guid lobbyId, Guid playerId, int index, int mistakes, string typed);
    Task RefreshPassage(Guid lobbyId);

    Task<List<ParticipantSnapshot>> GetSnapshot(Guid lobbyId);
    Task<IRaceSettings> GetRaceSettings(Guid lobbyId);
    Task<IRaceSettings?> GetRaceSettingsOrDefault(Guid lobbyId);

    void Register(Guid lobbyId, Race race);
    void Remove(Guid lobbyId);
}
