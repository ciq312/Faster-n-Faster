using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Api.UseCases.Services.Users;

public class UserProfileService(
    IUserRepository userRepo,
    IStatisticsRepository statsRepo,
    ILogger<UserProfileService> logger) : IUserProfileService
{
    public async Task<PlayerStatistics?> GetProfileAsync(Guid userId)
    {
        if (!await userRepo.IsUserRegistered(userId))
            throw new UserNotFoundException(userId);

        return await statsRepo.GetByUserIdAsync(userId);
    }

    public async Task ProcessRaceResultsAsync(IEnumerable<RaceParticipantResult> results)
    {
        foreach (var result in results)
        {
            if (!await userRepo.IsUserRegistered(result.LobbyPlayerId)) continue;

            var stats = await statsRepo.FindAsync(result.LobbyPlayerId);
            if (stats == null)
            {
                CreateRaceParticipantStatistics(result, out stats);
            }

            logger.LogDebug("Registering race result for {PlayerId}", result.LobbyPlayerId);
            stats.RegisterRace(result);
        }

        await statsRepo.SaveAsync();
    }

    private void CreateRaceParticipantStatistics(RaceParticipantResult result, out PlayerStatistics stats)
    {
        logger.LogDebug("Creating player statistics for {PlayerId}", result.LobbyPlayerId);
        stats = new PlayerStatistics(result.LobbyPlayerId);
        statsRepo.Add(stats);
    }
}
