using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Tests.Fakes;

public class FakeBanRepository : IBanRepository
{
    private readonly HashSet<Guid> banned = new();
    private readonly Dictionary<Guid, int> suspensionCounts = new();

    public int IsBannedCalls { get; private set; }
    public int BanCalls { get; private set; }
    public List<(Guid UserId, string Reason, DateTime ExpiresAt)> Suspensions { get; } = new();

    public void Seed(Guid userId) => banned.Add(userId);

    public Task<bool> IsBannedAsync(Guid userId)
    {
        IsBannedCalls++;
        return Task.FromResult(banned.Contains(userId));
    }

    public Task BanAsync(Guid userId, string? reason)
    {
        BanCalls++;
        banned.Add(userId);
        return Task.CompletedTask;
    }

    public Task<int> SuspendAsync(Guid userId, string reason, DateTime expiresAt)
    {
        Suspensions.Add((userId, reason, expiresAt));
        var count = suspensionCounts[userId] = suspensionCounts.GetValueOrDefault(userId) + 1;
        return Task.FromResult(count);
    }
}
