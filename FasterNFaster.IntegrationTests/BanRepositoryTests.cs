using FasterNFaster.Api.UseCases.Interfaces.Users;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FasterNFaster.IntegrationTests;

public class BanRepositoryTests(NoRateLimitApplicationFactory<Program> fixture) : IClassFixture<NoRateLimitApplicationFactory<Program>>, IAsyncLifetime
{
    private WebApplicationFactory<Program> app = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        app = fixture.CreateApp();
    }

    public async Task DisposeAsync() => await app.DisposeAsync();

    [Fact]
    public async Task SuspendAsync_CalledTwice_ReturnsIncrementingSuspensionCount()
    {
        var userId = Guid.NewGuid();

        await app.ExecuteScopedAsync<IBanRepository, int>(repo =>
            repo.SuspendAsync(userId, "first", DateTime.UtcNow.AddHours(1)));
        var secondCount = await app.ExecuteScopedAsync<IBanRepository, int>(repo =>
            repo.SuspendAsync(userId, "second", DateTime.UtcNow.AddHours(1)));

        Assert.Equal(2, secondCount);
    }

    [Fact]
    public async Task BanAsync_AfterExpiredSuspension_ResultsInPermanentBanWithoutThrowing()
    {
        var userId = Guid.NewGuid();
        await app.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(userId, "test suspension", DateTime.UtcNow.AddSeconds(-1)));

        await app.ExecuteScopedAsync<IBanRepository>(repo => repo.BanAsync(userId, "escalated to permanent"));

        var isBanned = await app.ExecuteScopedAsync<IBanRepository, bool>(repo => repo.IsBannedAsync(userId));
        Assert.True(isBanned);
    }

    [Fact]
    public async Task SuspendAsync_AgainstExistingPermanentBan_LeavesItPermanent()
    {
        var userId = Guid.NewGuid();
        await app.ExecuteScopedAsync<IBanRepository>(repo => repo.BanAsync(userId, "permanent ban"));

        await app.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(userId, "attempted suspension", DateTime.UtcNow.AddSeconds(-1)));

        // A permanent ban's ExpiresAt stays null, so it remains banned even though
        // the suspension's own expiry is already in the past.
        var isBanned = await app.ExecuteScopedAsync<IBanRepository, bool>(repo => repo.IsBannedAsync(userId));
        Assert.True(isBanned);
    }
}
