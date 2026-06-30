using System.Linq.Expressions;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Leaderboards;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure.Users;

public class LeaderboardService(AppDbContext context) : ILeaderboardService
{
    public async Task<LeaderboardPage> GetTopPlayersAsync(LeaderboardSort sort, bool descending, int page, int pageSize)
    {
        IQueryable<PlayerStatistics> query = context.Statistics.Include(s => s.User);

        int total = await query.CountAsync();

        var items = await ApplyOrder(query, sort, descending)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new LeaderboardPage(items, total);
    }

    // Typed switch keeps ordering EF-translatable and confined to whitelisted columns.
    private static IOrderedQueryable<PlayerStatistics> ApplyOrder(IQueryable<PlayerStatistics> query, LeaderboardSort sort, bool descending) => sort switch
    {
        LeaderboardSort.BestWpm => query.OrderByDir(s => s.BestWPM, descending),
        LeaderboardSort.AvgWpm => query.OrderByDir(s => s.AvgWPM, descending),
        LeaderboardSort.BestAccuracy => query.OrderByDir(s => s.BestAccuracy, descending),
        LeaderboardSort.AvgAccuracy => query.OrderByDir(s => s.AvgAccuracy, descending),
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
