using FasterNFaster.Api.Infrastructure.Db.Users;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Caching;

public class CachedBanRepositoryTests
{
    private readonly Api.Infrastructure.Db.Users.FakeBanRepository _inner = new();
    private readonly InMemoryCache _cache = new();
    private readonly CachedBanRepository _sut;

    public CachedBanRepositoryTests() => _sut = new CachedBanRepository(_inner, _cache);

    [Fact]
    public async Task IsBannedAsync_SecondCall_ServedFromCache()
    {
        var userId = Guid.NewGuid();

        var first = await _sut.IsBannedAsync(userId);
        var second = await _sut.IsBannedAsync(userId);

        Assert.False(first);
        Assert.False(second);
        Assert.Equal(1, _inner.IsBannedCalls);
    }

    [Fact]
    public async Task BanAsync_InvalidatesCachedResult()
    {
        var userId = Guid.NewGuid();
        await _sut.IsBannedAsync(userId); // caches "not banned"

        await _sut.BanAsync(userId, "cheating");
        var afterBan = await _sut.IsBannedAsync(userId);

        Assert.True(afterBan);
        Assert.Equal(1, _inner.BanCalls);
        Assert.Equal(2, _inner.IsBannedCalls); // re-queried after invalidation
    }
}
