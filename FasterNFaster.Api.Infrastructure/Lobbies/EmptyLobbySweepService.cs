using FasterNFaster.Api.UseCases.Lobbies.Cleanup;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Infrastructure.Lobbies;

public class EmptyLobbySweepService(
    ILobbyStore lobbies,
    EmptyLobbyRemover remover,
    TimeProvider timeProvider,
    IOptions<LobbyCleanupOptions> options,
    ILogger<EmptyLobbySweepService> logger) : BackgroundService
{
    private readonly LobbyCleanupOptions opts = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(opts.SweepInterval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await Sweep(stoppingToken);
    }

    public async Task Sweep(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var removed = 0;

        foreach (var lobbyId in lobbies.GetAllIds())
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                if (await lobbies.TryRead(lobbyId, l => l.IsAbandoned(now, opts.EmptyLobbyTtl))
                    && await remover.TryRemove(lobbyId))
                {
                    removed++;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Empty lobby sweep failed for lobby {LobbyId}", lobbyId);
            }
        }

        if (removed > 0) logger.LogInformation("Removed {Count} abandoned lobbies", removed);
    }
}
