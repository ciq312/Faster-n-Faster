using System.Linq.Expressions;
using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Infrastructure.Shared.Persistence;
using FasterNFaster.Api.UseCases.Leaderboards.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace FasterNFaster.Api.Infrastructure.Leaderboards;

public class LeaderboardRepository(AppDbContext context, TimeProvider timeProvider) : ILeaderboardRepository
{
    private static readonly Expression<Func<PlayerStatistics, float>> AvgWpm =
        s => s.RacesTyped == 0 ? 0 : s.SumWPM / s.RacesTyped;

    private static readonly Expression<Func<PlayerStatistics, float>> AvgAccuracy =
        s => s.RacesTyped == 0 ? 0 : s.SumAccuracy / s.RacesTyped;

    private static readonly Expression<Func<PlayerStatistics, LeaderboardEntry>> ToEntry =
        s => new LeaderboardEntry(
            s.Id,
            s.User.Nick,
            s.BestWPM,
            s.BestAccuracy,
            s.RacesTyped == 0 ? 0 : s.SumWPM / s.RacesTyped,
            s.RacesTyped == 0 ? 0 : s.SumAccuracy / s.RacesTyped,
            s.Wins,
            s.WordsTyped,
            s.RacesTyped);

    private static readonly Expression<Func<int, LeaderboardEntry, PlayerRank>> RankOf =
        (ahead, entry) => new PlayerRank(ahead + 1, entry);

    private static readonly Expression<Func<PlayerStatistics, PlayerStatistics, bool>> IdBefore =
        (s, me) => s.Id.CompareTo(me.Id) < 0;

    public async Task<LeaderboardPage> GetTopPlayersAsync(LeaderboardSort sort, bool descending, int page, int pageSize)
    {
        var query = Visible();

        var total = await query.CountAsync();

        var items = await ApplyOrder(query, sort, descending)
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToEntry)
            .ToListAsync();

        return new LeaderboardPage(items, total);
    }

    public async Task<PlayerRank?> GetPlayerRankAsync(Guid playerId, LeaderboardSort sort, bool descending, CancellationToken cancellationToken)
    {
        var visible = Visible();

        return await visible
            .Where(s => s.Id == playerId)
            .Select(RankSelector(visible, AheadOf(sort, descending)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<PlayerStatistics> Visible()
    {
        var now = timeProvider.GetUtcNow();
        return context.Statistics.AsNoTracking()
            .Where(s => !context.BannedPlayers.Any(b => b.UserId == s.Id && (b.ExpiresAt == null || b.ExpiresAt > now)));
    }

    private static Expression<Func<PlayerStatistics, PlayerRank>> RankSelector(
        IQueryable<PlayerStatistics> visible, Expression<Func<PlayerStatistics, PlayerStatistics, bool>> aheadOf)
    {
        var me = ToEntry.Parameters[0];
        var predicate = Expression.Lambda<Func<PlayerStatistics, bool>>(
            ReplacingExpressionVisitor.Replace(aheadOf.Parameters[1], me, aheadOf.Body),
            aheadOf.Parameters[0]);
        var ahead = Expression.Call(
            typeof(Queryable), nameof(Queryable.Count), [typeof(PlayerStatistics)], visible.Expression, Expression.Quote(predicate));

        return Expression.Lambda<Func<PlayerStatistics, PlayerRank>>(
            ReplacingExpressionVisitor.Replace(RankOf.Parameters, [ahead, ToEntry.Body], RankOf.Body),
            me);
    }

    // Mirrors ApplyOrder plus the id tiebreak, so a player's rank is their position in the list.
    private static Expression<Func<PlayerStatistics, PlayerStatistics, bool>> AheadOf(LeaderboardSort sort, bool descending) => sort switch
    {
        LeaderboardSort.BestWpm => AheadOf(s => s.BestWPM, descending),
        LeaderboardSort.AvgWpm => AheadOf(AvgWpm, descending),
        LeaderboardSort.BestAccuracy => AheadOf(s => s.BestAccuracy, descending),
        LeaderboardSort.AvgAccuracy => AheadOf(AvgAccuracy, descending),
        LeaderboardSort.Wins => AheadOf(s => s.Wins, descending),
        LeaderboardSort.WordsTyped => AheadOf(s => s.WordsTyped, descending),
        LeaderboardSort.RacesTyped => AheadOf(s => s.RacesTyped, descending),
        _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unsupported leaderboard sort.")
    };

    private static Expression<Func<PlayerStatistics, PlayerStatistics, bool>> AheadOf<TKey>(
        Expression<Func<PlayerStatistics, TKey>> key, bool descending)
    {
        var s = IdBefore.Parameters[0];
        var me = IdBefore.Parameters[1];
        var theirs = ReplacingExpressionVisitor.Replace(key.Parameters[0], s, key.Body);
        var mine = ReplacingExpressionVisitor.Replace(key.Parameters[0], me, key.Body);

        var better = descending ? Expression.GreaterThan(theirs, mine) : Expression.LessThan(theirs, mine);
        var tieBefore = Expression.AndAlso(Expression.Equal(theirs, mine), IdBefore.Body);

        return Expression.Lambda<Func<PlayerStatistics, PlayerStatistics, bool>>(Expression.OrElse(better, tieBefore), s, me);
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
