using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies.Races.Events;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Infrastructure;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;

public class SaveRaceResultHandler : IDomainEventHandler<RaceFinishedEvent>
{
    private readonly AppDbContext _db;

    public SaveRaceResultHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(RaceFinishedEvent domainEvent)
    {
        foreach (var result in domainEvent.Results)
        {
            var stats = await _db.Statistics.FindAsync(result.LobbyPlayerId);

            if (stats == null)
            {
                Log.Information("creating player statistics in db");
                stats = new PlayerStatistics(result.LobbyPlayerId);
                _db.Statistics.Add(stats);
            }
            Log.Information("registring new race in db");

            stats.RegisterRace(result);
        }

        await _db.SaveChangesAsync();
    }

}