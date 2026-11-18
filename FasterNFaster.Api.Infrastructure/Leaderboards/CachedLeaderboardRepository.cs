using FasterNFaster.Api.Infrastructure.Shared.Caching;
using FasterNFaster.Api.UseCases.Leaderboards.Ports;

namespace FasterNFaster.Api.Infrastructure.Leaderboards;

public class CachedLeaderboardRepository(ILeaderboardRepository inner, ICache cache) : ILeaderboardRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public Task<LeaderboardPage> GetTopPlayersAsync(LeaderboardSort sort, bool descending, int page, int pageSize)
    {
        var key = $"lb:{sort}:{descending}:{page}:{pageSize}";
        return cache.GetOrSetAsync(key, () => inner.GetTopPlayersAsync(sort, descending, page, pageSize), Ttl);
    }

    public Task<PlayerRank?> GetPlayerRankAsync(Guid playerId, LeaderboardSort sort, bool descending, CancellationToken cancellationToken) =>
        inner.GetPlayerRankAsync(playerId, sort, descending, cancellationToken);
}
