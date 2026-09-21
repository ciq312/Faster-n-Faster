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
<<<<<<< HEAD

    public async Task<bool> GetOrSetFlagAsync(string key, Func<Task<bool>> factory, TimeSpan ttl)
    {
        if (_store.TryGetValue(key, out var existing)) return (bool)existing!;

        var value = await factory();
        _store[key] = value;
        return value;
    }

    public Task RemoveAsync(params string[] keys)
    {
        foreach (var key in keys) _store.Remove(key);
        return Task.CompletedTask;
    }

    public Task<long> BumpVersionAsync(string key)
    {
        long next = _versions.GetValueOrDefault(key) + 1;
        _versions[key] = next;
        return Task.FromResult(next);
    }

    public Task<long> GetVersionAsync(string key) => Task.FromResult(_versions.GetValueOrDefault(key));
=======
>>>>>>> dev
}
