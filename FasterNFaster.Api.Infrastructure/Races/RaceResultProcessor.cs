using FasterNFaster.Api.Infrastructure.Shared.Telemetry;
using FasterNFaster.Api.UseCases.Races.Ports;
using FasterNFaster.Api.UseCases.Shared;
using FasterNFaster.Api.UseCases.Users.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FasterNFaster.Api.Infrastructure.Races;

public class RaceResultProcessor(
    IRaceResultQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<RaceResultProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RacePerformanceMetrics.RecordResultBatches(0, persisted: true);
        RacePerformanceMetrics.RecordResultBatches(0, persisted: false);

        await foreach (var results in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var profileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
                    await profileService.ProcessRaceResultsAsync(results);
                }

                RacePerformanceMetrics.RecordResultBatches(1, persisted: true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist race results");
                logger.LogFailure(SafeTelemetryLog.RaceResultPersistenceFailed, ex);
                RacePerformanceMetrics.RecordResultBatches(1, persisted: false);
            }
        }
    }
}
