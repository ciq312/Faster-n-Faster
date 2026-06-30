using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Leaderboards;

namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface ILeaderboardService
{
    Task<LeaderboardPage> GetTopPlayersAsync(LeaderboardSort sort, bool descending, int page, int pageSize);
}

public record LeaderboardPage(IReadOnlyList<PlayerStatistics> Items, int TotalPlayers);
