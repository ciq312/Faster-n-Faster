using FasterNFaster.Api.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure;

public class PostgresStatisticsRepository(AppDbContext context) : IStatisticsRepository
{
    public Task<PlayerStatistics?> GetByUserIdAsync(Guid userId) =>
        context.Statistics.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == userId);
}
