using FasterNFaster.Api.Infrastructure.Shared.Caching;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace FasterNFaster.IntegrationTests;

public class RedisCacheTests(NoRateLimitApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ShortTtl = TimeSpan.FromMilliseconds(200);

    private RedisCache cache = null!;
    private int runs;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        cache = App.Services.GetRequiredService<RedisCache>();
    }

    [Fact]
    public async Task Hit_ReturnsCachedValueWithoutRunningFactory()
    {
        var key = NewKey();
        await cache.GetOrSetAsync(key, CountingFactory, Ttl);

        var hit = await cache.GetOrSetAsync(key, CountingFactory, Ttl);

        hit.ShouldBe("value1");
        runs.ShouldBe(1);
    }

    [Fact]
    public async Task EntryPastTtl_RunsFactoryAgain()
    {
        var key = NewKey();
        await cache.GetOrSetAsync(key, CountingFactory, ShortTtl);

        await Task.Delay(ShortTtl * 2);

        (await cache.GetOrSetAsync(key, CountingFactory, ShortTtl)).ShouldBe("value2");
        runs.ShouldBe(2);
    }

    [Fact]
    public async Task ThrowingFactory_NextCallRunsFactoryAgain()
    {
        var key = NewKey();

        await Should.ThrowAsync<InvalidOperationException>(
            cache.GetOrSetAsync<string>(key, () => throw new InvalidOperationException(), Ttl));

        (await cache.GetOrSetAsync(key, CountingFactory, Ttl)).ShouldBe("value1");
        runs.ShouldBe(1);
    }

    private static string NewKey() => $"test:{Guid.NewGuid():N}";

    private Task<string> CountingFactory() => Task.FromResult($"value{++runs}");
}
