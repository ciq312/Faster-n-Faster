namespace FasterNFaster.Api.UseCases.Leaderboards.Ports;

public interface ILeaderboardRepository
{
    Task<LeaderboardPage> GetTopPlayersAsync(LeaderboardSort sort, bool descending, int page, int pageSize);
    Task<PlayerRank?> GetPlayerRankAsync(Guid playerId, LeaderboardSort sort, bool descending, CancellationToken cancellationToken);
}
