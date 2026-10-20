using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races;

namespace FasterNFaster.Api.UseCases.Users.Services;

public interface IUserProfileService
{
    Task<PlayerStatistics?> GetProfileAsync(Guid userId);
    Task ProcessRaceResultsAsync(IEnumerable<RaceParticipantResult> results);
}
