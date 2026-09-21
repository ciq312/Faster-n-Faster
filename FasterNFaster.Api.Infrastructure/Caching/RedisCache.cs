using StackExchange.Redis;

namespace FasterNFaster.Api.Infrastructure.Caching;

public class RedisCache(IConnectionMultiplexer redis) : ICache
{
    private readonly IDatabase db = redis.GetDatabase();

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan ttl) where T : class
    {
        var cached = await db.StringGetAsync(key);
        if (cached.HasValue) return CacheSerializer.Deserialize<T>(cached!);

        var value = await factory();
        if (value is not null)
            await db.StringSetAsync(key, CacheSerializer.Serialize(value), ttl);

        return value;
    }

    public async Task<bool> GetOrSetFlagAsync(string key, Func<Task<bool>> factory, TimeSpan ttl)
    {
        var cached = await db.StringGetAsync(key);
        if (cached.HasValue) return cached == "1";

        var value = await factory();
        await db.StringSetAsync(key, value ? "1" : "0", ttl);
        return value;
    }

    public Task RemoveAsync(params string[] keys) =>
        db.KeyDeleteAsync(Array.ConvertAll(keys, key => (RedisKey)key));

    public Task<long> BumpVersionAsync(string key) => db.StringIncrementAsync(key);

    public async Task<long> GetVersionAsync(string key)
    {
        var value = await db.StringGetAsync(key);
        return value.HasValue ? (long)value : 0;
    }
}
