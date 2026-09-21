using FasterNFaster.Api.Infrastructure.Caching;

namespace FasterNFaster.Tests.Fakes;

public class InMemoryCache : ICache
{
    private readonly Dictionary<string, object?> _store = new();

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan ttl) where T : class
    {
        if (_store.TryGetValue(key, out var existing)) return (T?)existing;

        var value = await factory();
        if (value is not null) _store[key] = value;
        return value;
    }
}
