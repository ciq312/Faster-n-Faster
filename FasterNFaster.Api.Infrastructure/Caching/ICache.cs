namespace FasterNFaster.Api.Infrastructure.Caching;

public interface ICache
{
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan ttl) where T : class;
}
