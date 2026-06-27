using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface IStatisticsRepository
{
    Task<PlayerStatistics?> GetByUserIdAsync(Guid userId);
    Task<PlayerStatistics?> FindAsync(Guid userId);
    void Add(PlayerStatistics stats);
    Task SaveAsync();
}
