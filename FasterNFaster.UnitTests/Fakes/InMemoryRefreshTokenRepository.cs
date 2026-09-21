using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Interfaces.Auth;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ConcurrentDictionary<string, Guid> tokenToUser = new();
    private readonly ConcurrentDictionary<string, DateTime> expiries = new();

    public Task Issue(Guid userId, string refreshToken, TimeSpan ttl)
    {
        tokenToUser[refreshToken] = userId;
        expiries[refreshToken] = DateTime.UtcNow.Add(ttl);
        return Task.CompletedTask;
    }

    public Task<Guid?> RotateRefreshToken(string oldRefreshToken, string newRefreshToken, TimeSpan? ttl)
    {
        if (!IsValid(oldRefreshToken)) return Task.FromResult<Guid?>(null);

        var userId = tokenToUser[oldRefreshToken];

        var newTtl = ttl ?? (expiries[oldRefreshToken] - DateTime.UtcNow);
        if (newTtl <= TimeSpan.Zero) return Task.FromResult<Guid?>(null);

        Remove(oldRefreshToken);
        tokenToUser[newRefreshToken] = userId;
        expiries[newRefreshToken] = DateTime.UtcNow.Add(newTtl);
        return Task.FromResult<Guid?>(userId);
    }

    public Task Invalidate(string refreshToken)
    {
        Remove(refreshToken);
        return Task.CompletedTask;
    }

    public Task InvalidateAll(Guid userId)
    {
        foreach (var token in tokenToUser.Where(kv => kv.Value == userId).Select(kv => kv.Key).ToArray())
            Remove(token);
        return Task.CompletedTask;
    }

    private bool IsValid(string token) =>
        tokenToUser.ContainsKey(token) && expiries.TryGetValue(token, out var exp) && exp > DateTime.UtcNow;

    private void Remove(string token)
    {
        tokenToUser.TryRemove(token, out _);
        expiries.TryRemove(token, out _);
    }
}
