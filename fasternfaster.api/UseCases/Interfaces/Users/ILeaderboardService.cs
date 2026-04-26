using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface ILeaderboardService
{
    Task<IEnumerable<PlayerStatistics>> GetTopPlayersAsync(string criteria, bool isDescending, int playersCount);
}