using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.Utils;
using Microsoft.EntityFrameworkCore;
namespace FasterNFaster.Api.UseCases.Services;

public class LeaderboardService(AppDbContext context) : ILeaderboardService
{
    private readonly AppDbContext _context = context;
    public async Task<IEnumerable<PlayerStatistics>> GetTopPlayersAsync(string criteria, bool isDescending, int playersCount)
    {
        IQueryable<PlayerStatistics> query = _context.Statistics.Include(s => s.User);

        var filter = FilterBuildersUtils.GetTopPlayersFilter(query, criteria, isDescending, playersCount);

        return await filter.ToListAsync();
    }
}
