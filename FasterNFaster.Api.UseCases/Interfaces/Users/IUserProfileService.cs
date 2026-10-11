using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface IUserProfileService
{
    Task<PlayerStatistics?> GetProfileAsync(Guid userId);
    Task ProcessRaceResultsAsync(IEnumerable<RaceParticipantResult> results);
}
