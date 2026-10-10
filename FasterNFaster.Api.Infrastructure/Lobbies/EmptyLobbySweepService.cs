using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.Cleanup;
using FasterNFaster.Api.Web.Options.Lobbies;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Infrastructure.Lobbies;

public class EmptyLobbySweepService(
    ILobbyRepository lobbyStore,
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

        foreach (var lobby in lobbyStore.GetAll().Where(l => l.IsAbandoned(now, opts.EmptyLobbyTtl)))
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                if (await remover.TryRemove(lobby.Id)) removed++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Empty lobby sweep failed for lobby {LobbyId}", lobby.Id);
            }
        }

        if (removed > 0) logger.LogInformation("Removed {Count} abandoned lobbies", removed);
    }
}
