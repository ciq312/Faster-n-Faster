using System.Text.Json;
using StackExchange.Redis;

namespace FasterNFaster.Api.Infrastructure.Shared.Caching;

public class RedisCache(IConnectionMultiplexer redis) : ICache
{
    private readonly IDatabase db = redis.GetDatabase();

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) where T : class
    {
        var cached = await db.StringGetAsync(key);
        if (cached.HasValue && JsonSerializer.Deserialize<T>(cached.ToString()) is { } hit) return hit;

        var value = await factory();
        await db.StringSetAsync(key, JsonSerializer.Serialize(value), ttl);
        return value;
    }
}
