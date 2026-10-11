using FasterNFaster.Api.UseCases.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FasterNFaster.IntegrationTests;

public class RedisRefreshTokenRepositoryTests(NoRateLimitApplicationFactory<Program> fixture) : IClassFixture<NoRateLimitApplicationFactory<Program>>, IAsyncLifetime
{
    private const int ConcurrentRotations = 10;
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);
    private static readonly TimeSpan ShortTtl = TimeSpan.FromMilliseconds(200);

    private WebApplicationFactory<Program> app = null!;
    private IRefreshTokenRepository repo = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        app = fixture.CreateApp();
        repo = app.Services.GetRequiredService<IRefreshTokenRepository>();
    }

    public async Task DisposeAsync() => await app.DisposeAsync();

    [Fact]
    public async Task IssuedToken_RotatesToOwner()
    {
        var owner = Guid.NewGuid();
        await repo.Issue(owner, "old", Ttl);

        var rotatedOwner = await repo.RotateRefreshToken("old", "new", Ttl);

        Assert.Equal(owner, rotatedOwner);
    }

    [Fact]
    public async Task RotatedToken_CannotRotateAgain()
    {
        await repo.Issue(Guid.NewGuid(), "old", Ttl);
        await repo.RotateRefreshToken("old", "new", Ttl);

        Assert.Null(await repo.RotateRefreshToken("old", "new-again", Ttl));
    }

    [Fact]
    public async Task ReplacementToken_RotatesToOwner()
    {
        var owner = Guid.NewGuid();
        await repo.Issue(owner, "old", Ttl);
        await repo.RotateRefreshToken("old", "new", Ttl);

        Assert.Equal(owner, await repo.RotateRefreshToken("new", "newest", Ttl));
    }

    [Fact]
    public async Task UnknownToken_DoesNotRotate()
    {
        Assert.Null(await repo.RotateRefreshToken("unknown", "new", Ttl));
    }

    [Fact]
    public async Task InvalidatedToken_DoesNotRotate()
    {
        await repo.Issue(Guid.NewGuid(), "old", Ttl);

        await repo.Invalidate("old");

        Assert.Null(await repo.RotateRefreshToken("old", "new", Ttl));
    }

    [Fact]
    public async Task InvalidateAll_RevokesEveryTokenOfUser()
    {
        var owner = Guid.NewGuid();
        await repo.Issue(owner, "device-1", Ttl);
        await repo.Issue(owner, "device-2", Ttl);

        await repo.InvalidateAll(owner);

        Assert.Null(await repo.RotateRefreshToken("device-1", "device-1-new", Ttl));
        Assert.Null(await repo.RotateRefreshToken("device-2", "device-2-new", Ttl));
    }

    [Fact]
    public async Task InvalidateAll_RevokesTokenRotatedBeforeIt()
    {
        var owner = Guid.NewGuid();
        await repo.Issue(owner, "old", Ttl);
        await repo.RotateRefreshToken("old", "new", Ttl);

        await repo.InvalidateAll(owner);

        Assert.Null(await repo.RotateRefreshToken("new", "newest", Ttl));
    }

    [Fact]
    public async Task TokenIssuedAfterInvalidateAll_RotatesToOwner()
    {
        var owner = Guid.NewGuid();
        await repo.InvalidateAll(owner);
        await repo.Issue(owner, "fresh", Ttl);

        Assert.Equal(owner, await repo.RotateRefreshToken("fresh", "new", Ttl));
    }

    [Fact]
    public async Task RotatingOneDevicesToken_LeavesOtherDevicesTokenValid()
    {
        var owner = Guid.NewGuid();
        await repo.Issue(owner, "device-1", Ttl);
        await repo.Issue(owner, "device-2", Ttl);

        await repo.RotateRefreshToken("device-1", "device-1-new", Ttl);

        Assert.Equal(owner, await repo.RotateRefreshToken("device-2", "device-2-new", Ttl));
    }

    [Fact]
    public async Task InvalidateAll_LeavesOtherUsersTokensValid()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await repo.Issue(userA, "a", Ttl);
        await repo.Issue(userB, "b", Ttl);

        await repo.InvalidateAll(userA);

        Assert.Null(await repo.RotateRefreshToken("a", "a-new", Ttl));
        Assert.Equal(userB, await repo.RotateRefreshToken("b", "b-new", Ttl));
    }

    [Fact]
    public async Task IssuedTokenPastTtl_DoesNotRotate()
    {
        await repo.Issue(Guid.NewGuid(), "old", ShortTtl);

        await Task.Delay(ShortTtl * 2);

        Assert.Null(await repo.RotateRefreshToken("old", "new", Ttl));
    }

    [Fact]
    public async Task ReplacementTokenPastTtl_DoesNotRotate()
    {
        await repo.Issue(Guid.NewGuid(), "old", Ttl);
        await repo.RotateRefreshToken("old", "new", ShortTtl);

        await Task.Delay(ShortTtl * 2);

        Assert.Null(await repo.RotateRefreshToken("new", "newest", Ttl));
    }

    [Fact]
    public async Task ConcurrentRotationsOfSameToken_ExactlyOneSucceeds()
    {
        var owner = Guid.NewGuid();

        var results = await IssueThenRotateConcurrentlyAsync(owner);

        Assert.Equal(owner, Assert.Single(results, r => r is not null));
    }

    [Fact]
    public async Task ConcurrentRotationsOfSameToken_OnlyWinnersNewTokenIsStored()
    {
        var results = await IssueThenRotateConcurrentlyAsync(Guid.NewGuid());

        var rotations = await Task.WhenAll(results.Select((_, i) => repo.RotateRefreshToken($"new-{i}", $"next-{i}", Ttl)));

        Assert.Equal(results, rotations);
    }

    private async Task<Guid?[]> IssueThenRotateConcurrentlyAsync(Guid owner)
    {
        await repo.Issue(owner, "old", Ttl);

        return await Task.WhenAll(
            Enumerable.Range(0, ConcurrentRotations).Select(i => repo.RotateRefreshToken("old", $"new-{i}", Ttl)));
    }
}
