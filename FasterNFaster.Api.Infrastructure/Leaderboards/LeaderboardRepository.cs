using System.Linq.Expressions;
using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Infrastructure.Shared.Persistence;
using FasterNFaster.Api.UseCases.Leaderboards.Ports;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure.Leaderboards;

public class LeaderboardRepository(AppDbContext context, TimeProvider timeProvider) : ILeaderboardRepository
{
    private static readonly Expression<Func<PlayerStatistics, float>> AvgWpm =
        s => s.RacesTyped == 0 ? 0 : s.SumWPM / s.RacesTyped;

    private static readonly Expression<Func<PlayerStatistics, float>> AvgAccuracy =
        s => s.RacesTyped == 0 ? 0 : s.SumAccuracy / s.RacesTyped;

    public async Task<LeaderboardPage> GetTopPlayersAsync(LeaderboardSort sort, bool descending, int page, int pageSize)
    {
        var now = timeProvider.GetUtcNow();
        var query = context.Statistics.AsNoTracking()
            .Where(s => !context.BannedPlayers.Any(b => b.UserId == s.Id && (b.ExpiresAt == null || b.ExpiresAt > now)));

        var total = await query.CountAsync();

        var items = await ApplyOrder(query, sort, descending)
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new LeaderboardEntry(
                s.Id,
                s.User.Nick,
                s.BestWPM,
                s.BestAccuracy,
                s.RacesTyped == 0 ? 0 : s.SumWPM / s.RacesTyped,
                s.RacesTyped == 0 ? 0 : s.SumAccuracy / s.RacesTyped,
                s.Wins,
                s.WordsTyped,
                s.RacesTyped))
            .ToListAsync();

        return new LeaderboardPage(items, total);
    }

    // Typed switch keeps ordering EF-translatable and confined to whitelisted columns.
    private static IOrderedQueryable<PlayerStatistics> ApplyOrder(IQueryable<PlayerStatistics> query, LeaderboardSort sort, bool descending) => sort switch
    {
        LeaderboardSort.BestWpm => query.OrderByDir(s => s.BestWPM, descending),
        LeaderboardSort.AvgWpm => query.OrderByDir(AvgWpm, descending),
        LeaderboardSort.BestAccuracy => query.OrderByDir(s => s.BestAccuracy, descending),
        LeaderboardSort.AvgAccuracy => query.OrderByDir(AvgAccuracy, descending),
        LeaderboardSort.Wins => query.OrderByDir(s => s.Wins, descending),
        LeaderboardSort.WordsTyped => query.OrderByDir(s => s.WordsTyped, descending),
        LeaderboardSort.RacesTyped => query.OrderByDir(s => s.RacesTyped, descending),
        _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unsupported leaderboard sort.")
    };
}

internal static class QueryableOrderingExtensions
{
    public static IOrderedQueryable<T> OrderByDir<T, TKey>(this IQueryable<T> query, Expression<Func<T, TKey>> key, bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);
}
