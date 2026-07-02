using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Api.Infrastructure.Races;

public class RaceResultProcessor(
    IRaceResultQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<RaceResultProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var results in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var profileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
                await profileService.ProcessRaceResultsAsync(results);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist race results");
            }
        }
    }
}
