using FasterNFaster.Api.UseCases.AntiCheat.Ports;

namespace FasterNFaster.IntegrationTests;

public class BanRepositoryTests(NoRateLimitApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    [Fact]
    public async Task ActiveSuspension_CountsAsBanned()
    {
        var userId = Guid.NewGuid();
        await App.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(userId, "cooldown", DateTime.UtcNow.AddHours(1)));

        Assert.True(await App.ExecuteScopedAsync<IBanRepository, bool>(repo => repo.IsBannedAsync(userId)));
    }

    [Fact]
    public async Task ExpiredSuspension_NoLongerCountsAsBanned()
    {
        var userId = Guid.NewGuid();
        await App.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(userId, "cooldown", DateTime.UtcNow.AddSeconds(-1)));

        Assert.False(await App.ExecuteScopedAsync<IBanRepository, bool>(repo => repo.IsBannedAsync(userId)));
    }

    [Fact]
    public async Task SuspendAsync_CalledTwice_ReturnsIncrementingSuspensionCount()
    {
        var userId = Guid.NewGuid();

        await App.ExecuteScopedAsync<IBanRepository, int>(repo =>
            repo.SuspendAsync(userId, "first", DateTime.UtcNow.AddHours(1)));
        var secondCount = await App.ExecuteScopedAsync<IBanRepository, int>(repo =>
            repo.SuspendAsync(userId, "second", DateTime.UtcNow.AddHours(1)));

        Assert.Equal(2, secondCount);
    }

    [Fact]
    public async Task BanAsync_AfterExpiredSuspension_ResultsInPermanentBanWithoutThrowing()
    {
        var userId = Guid.NewGuid();
        await App.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(userId, "test suspension", DateTime.UtcNow.AddSeconds(-1)));

        await App.ExecuteScopedAsync<IBanRepository>(repo => repo.BanAsync(userId, "escalated to permanent"));

        var isBanned = await App.ExecuteScopedAsync<IBanRepository, bool>(repo => repo.IsBannedAsync(userId));
        Assert.True(isBanned);
    }

    [Fact]
    public async Task SuspendAsync_AgainstExistingPermanentBan_LeavesItPermanent()
    {
        var userId = Guid.NewGuid();
        await App.ExecuteScopedAsync<IBanRepository>(repo => repo.BanAsync(userId, "permanent ban"));

        await App.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(userId, "attempted suspension", DateTime.UtcNow.AddSeconds(-1)));

        // A permanent ban's ExpiresAt stays null, so it remains banned even though
        // the suspension's own expiry is already in the past.
        var isBanned = await App.ExecuteScopedAsync<IBanRepository, bool>(repo => repo.IsBannedAsync(userId));
        Assert.True(isBanned);
    }
}
