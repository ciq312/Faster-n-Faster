using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Api.Infrastructure.Users;

public class StrikeRegistry : IStrikeRegistry
{
    private readonly ConcurrentDictionary<(Guid LobbyId, Guid UserId), int> raceViolations = new();
    private readonly ConcurrentDictionary<Guid, List<DateTime>> strikes = new();

    public int RecordRaceViolation(Guid lobbyId, Guid userId) =>
        raceViolations.AddOrUpdate((lobbyId, userId), 1, (_, count) => count + 1);

    public void ClearRace(Guid lobbyId)
    {
        foreach (var key in raceViolations.Keys.Where(k => k.LobbyId == lobbyId).ToList())
            raceViolations.TryRemove(key, out _);
    }

    public int RecordStrike(Guid userId, DateTime now, TimeSpan window)
    {
        var timestamps = strikes.GetOrAdd(userId, _ => new List<DateTime>());
        lock (timestamps)
        {
            timestamps.Add(now);
            timestamps.RemoveAll(t => t <= now - window);
            return timestamps.Count;
        }
    }

    public void ClearStrikes(Guid userId) => strikes.TryRemove(userId, out _);
}
