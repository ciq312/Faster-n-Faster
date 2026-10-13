using FasterNFaster.Api.Infrastructure.Shared.Caching;

namespace FasterNFaster.UnitTests.Fakes;

public class InMemoryCache : ICache
{
    private readonly Dictionary<string, object> store = new();

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) where T : class
    {
        if (store.TryGetValue(key, out var existing)) return (T)existing;

        var value = await factory();
        store[key] = value;
        return value;
    }
}
