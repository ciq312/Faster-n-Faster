using FasterNFaster.Api.Domain.Entities;

namespace FasterNFaster.Api.UseCases.Users.Ports;

public interface IStatisticsRepository
{
    Task<PlayerStatistics?> GetByUserIdAsync(Guid userId);
    Task<PlayerStatistics?> FindAsync(Guid userId);
    void Add(PlayerStatistics stats);
    Task SaveAsync();
}
