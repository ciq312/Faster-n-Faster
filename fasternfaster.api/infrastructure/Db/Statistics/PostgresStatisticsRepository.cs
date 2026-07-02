using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure;

public class PostgresStatisticsRepository(AppDbContext context) : IStatisticsRepository
{
    public Task<PlayerStatistics?> GetByUserIdAsync(Guid userId) =>
        context.Statistics.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == userId);

    public async Task<PlayerStatistics?> FindAsync(Guid userId) =>
        await context.Statistics.FindAsync(userId);

    public void Add(PlayerStatistics stats) => context.Statistics.Add(stats);

    public Task SaveAsync() => context.SaveChangesAsync();
}
