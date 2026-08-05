namespace FasterNFaster.Api.Infrastructure.Caching;

public interface ICache
{
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan ttl) where T : class;
    Task<bool> GetOrSetFlagAsync(string key, Func<Task<bool>> factory, TimeSpan ttl);
    Task RemoveAsync(params string[] keys);
    Task<long> BumpVersionAsync(string key);
    Task<long> GetVersionAsync(string key);
}
