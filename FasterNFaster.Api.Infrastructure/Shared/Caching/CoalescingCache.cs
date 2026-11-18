using System.Collections.Concurrent;

namespace FasterNFaster.Api.Infrastructure.Shared.Caching;

public class CoalescingCache(ICache inner) : ICache
{
    private readonly ConcurrentDictionary<string, Lazy<Task>> loads = new();

    public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) where T : class
    {
        var load = loads.GetOrAdd(key, _ => new Lazy<Task>(() => LoadAsync(key, factory, ttl)));
        return (Task<T>)load.Value;
    }

    private async Task<T> LoadAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) where T : class
    {
        try
        {
            return await inner.GetOrSetAsync(key, factory, ttl);
        }
        finally
        {
            // Removed before the task settles, so a caller arriving after a failure starts a fresh load.
            loads.TryRemove(key, out _);
        }
    }
}
