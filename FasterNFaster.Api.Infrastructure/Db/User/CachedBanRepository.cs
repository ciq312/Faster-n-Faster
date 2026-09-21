using FasterNFaster.Api.Infrastructure.Caching;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Api.Infrastructure.Db.Users;

public class CachedBanRepository(IBanRepository inner, ICache cache) : IBanRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    private static string Key(Guid userId) => $"ban:{userId}";

    public Task<bool> IsBannedAsync(Guid userId) =>
        cache.GetOrSetFlagAsync(Key(userId), () => inner.IsBannedAsync(userId), Ttl);

    public async Task BanAsync(Guid userId, string? reason)
    {
        await inner.BanAsync(userId, reason);
        await cache.RemoveAsync(Key(userId));
    }
}
