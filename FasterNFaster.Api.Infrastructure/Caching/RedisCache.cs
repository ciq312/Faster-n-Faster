using System.Text.Json;
using StackExchange.Redis;

namespace FasterNFaster.Api.Infrastructure.Caching;

public class RedisCache(IConnectionMultiplexer redis) : ICache
{
    private readonly IDatabase db = redis.GetDatabase();

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan ttl) where T : class
    {
        var cached = await db.StringGetAsync(key);
        if (cached.HasValue) return JsonSerializer.Deserialize<T>(cached.ToString());

        var value = await factory();
        if (value is not null)
            await db.StringSetAsync(key, JsonSerializer.Serialize(value), ttl);

        return value;
    }
}
