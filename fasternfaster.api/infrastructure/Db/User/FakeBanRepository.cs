using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.Infrastructure.Db.User;

public class FakeBanRepository : IBanRepository
{
    private readonly HashSet<Guid> _banned = new();

    public int IsBannedCalls { get; private set; }
    public int BanCalls { get; private set; }

    public void Seed(Guid userId) => _banned.Add(userId);

    public Task<bool> IsBannedAsync(Guid userId)
    {
        IsBannedCalls++;
        return Task.FromResult(_banned.Contains(userId));
    }

    public Task BanAsync(Guid userId, string? reason)
    {
        BanCalls++;
        _banned.Add(userId);
        return Task.CompletedTask;
    }
}
