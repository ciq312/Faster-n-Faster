using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Tests.Fakes;

public class FakeStatisticsRepository : IStatisticsRepository
{
    private readonly Dictionary<Guid, PlayerStatistics> store = new();

    public int GetByUserIdCalls { get; private set; }
    public int FindCalls { get; private set; }
    public int SaveCalls { get; private set; }

    public void Seed(PlayerStatistics stats) => store[stats.Id] = stats;

    public Task<PlayerStatistics?> GetByUserIdAsync(Guid userId)
    {
        GetByUserIdCalls++;
        return Task.FromResult(store.GetValueOrDefault(userId));
    }

    public Task<PlayerStatistics?> FindAsync(Guid userId)
    {
        FindCalls++;
        return Task.FromResult(store.GetValueOrDefault(userId));
    }

    public void Add(PlayerStatistics stats) => store[stats.Id] = stats;

    public Task SaveAsync()
    {
        SaveCalls++;
        return Task.CompletedTask;
    }
}
