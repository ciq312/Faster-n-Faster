using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface IUserProfileService
{
    Task<PlayerStatistics?> GetProfileAsync(Guid userId);
    Task ProcessRaceResultsAsync(IEnumerable<RaceParticipantResult> results);
}
