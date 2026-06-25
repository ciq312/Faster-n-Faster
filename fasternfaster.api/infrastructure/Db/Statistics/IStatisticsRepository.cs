using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.Infrastructure;

public interface IStatisticsRepository
{
    Task<PlayerStatistics?> GetByUserIdAsync(Guid userId);
}
