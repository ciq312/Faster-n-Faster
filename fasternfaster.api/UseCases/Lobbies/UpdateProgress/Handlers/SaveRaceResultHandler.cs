using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies.Races.Events;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Infrastructure;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;

public class SaveRaceResultHandler(AppDbContext db, IUserRepository repo, ILogger<SaveRaceResultHandler> logger) : IDomainEventHandler<RaceFinishedEvent>
{
    public async Task Handle(RaceFinishedEvent domainEvent)
    {
        foreach (var result in domainEvent.Results)
        {
            if (!await repo.IsUserRegistred(result.LobbyPlayerId)) continue;

            var stats = await db.Statistics.FindAsync(result.LobbyPlayerId);

            if (stats == null)
            {
                logger.LogDebug("Creating player statistics for {PlayerId}", result.LobbyPlayerId);
                stats = new PlayerStatistics(result.LobbyPlayerId);
                db.Statistics.Add(stats);
            }

            logger.LogDebug("Registering race result for {PlayerId}", result.LobbyPlayerId);
            stats.RegisterRace(result);
        }

        await db.SaveChangesAsync();
    }
}