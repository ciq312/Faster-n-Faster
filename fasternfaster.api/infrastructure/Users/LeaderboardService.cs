using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.Utils;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure.Users;

public class LeaderboardService(AppDbContext context) : ILeaderboardService
{
    public async Task<IEnumerable<PlayerStatistics>> GetTopPlayersAsync(string criteria, bool isDescending, int playersCount)
    {
        IQueryable<PlayerStatistics> query = context.Statistics.Include(s => s.User);
        var filter = FilterBuildersUtils.GetTopPlayersFilter(query, criteria, isDescending, playersCount);
        return await filter.ToListAsync();
    }
}
