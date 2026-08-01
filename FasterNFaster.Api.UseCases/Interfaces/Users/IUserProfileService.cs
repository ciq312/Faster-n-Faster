using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface IUserProfileService
{
    Task<PlayerStatistics?> GetProfileAsync(Guid userId);
    Task ProcessRaceResultsAsync(IEnumerable<RaceParticipantResult> results);
}
