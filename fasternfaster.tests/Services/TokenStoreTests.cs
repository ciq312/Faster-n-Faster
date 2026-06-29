using FasterNFaster.Api.Infrastructure.Auth;

namespace FasterNFaster.Tests.Services;

public class InMemoryRefreshTokenRepositoryTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    private static InMemoryRefreshTokenRepository CreateStore() => new();

    [Fact]
    public async Task Issue_ThenRotate_ReturnsOwner()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();
        await store.Issue(userId, "old", Ttl);

        var owner = await store.RotateRefreshToken("old", "new", Ttl);

        Assert.Equal(userId, owner);
    }

    [Fact]
    public async Task Rotate_ConsumesOldToken()
    {
        var store = CreateStore();
        await store.Issue(Guid.NewGuid(), "old", Ttl);

        await store.RotateRefreshToken("old", "new", Ttl);

        Assert.Null(await store.RotateRefreshToken("old", "new2", Ttl));
    }

    [Fact]
    public async Task Rotate_UnknownToken_ReturnsNull()
    {
        var store = CreateStore();

        Assert.Null(await store.RotateRefreshToken("nope", "new", Ttl));
    }

    [Fact]
    public async Task Invalidate_TokenCanNoLongerRotate()
    {
        var store = CreateStore();
        await store.Issue(Guid.NewGuid(), "t", Ttl);

        await store.Invalidate("t");

        Assert.Null(await store.RotateRefreshToken("t", "new", Ttl));
    }

    [Fact]
    public async Task InvalidateAll_TokenCanNoLongerRotate()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();
        await store.Issue(userId, "t", Ttl);

        await store.InvalidateAll(userId);

        Assert.Null(await store.RotateRefreshToken("t", "new", Ttl));
    }

    [Fact]
    public async Task InvalidateAll_OtherUsersTokensUnaffected()
    {
        var store = CreateStore();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await store.Issue(userA, "a", Ttl);
        await store.Issue(userB, "b", Ttl);

        await store.InvalidateAll(userA);

        Assert.Null(await store.RotateRefreshToken("a", "a2", Ttl));
        Assert.Equal(userB, await store.RotateRefreshToken("b", "b2", Ttl));
    }

    [Fact]
    public async Task InvalidateAll_NoEntry_NoThrow()
    {
        var store = CreateStore();

        await store.InvalidateAll(Guid.NewGuid());
    }

    [Fact]
    public async Task Rotate_MultipleTokensPerUser_OnlyRotatedOneIsConsumed()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();
        await store.Issue(userId, "device-1", Ttl);
        await store.Issue(userId, "device-2", Ttl);

        await store.RotateRefreshToken("device-1", "device-1-new", Ttl);

        Assert.Equal(userId, await store.RotateRefreshToken("device-2", "device-2-new", Ttl));
    }
}
