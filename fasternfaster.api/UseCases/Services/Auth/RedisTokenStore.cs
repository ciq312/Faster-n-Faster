using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Options.JwtOptions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FasterNFaster.Api.UseCases.Services.Auth;

public class RedisTokenStore : ITokenStore
{
    private readonly IDatabase db;
    private readonly TimeSpan ttl;

    public RedisTokenStore(IConnectionMultiplexer redis, IOptions<JwtOptions> options)
    {
        db = redis.GetDatabase();
        ttl = options.Value.RefreshTokenLifetime;
    }

    private static string TokenKey(string token) => $"auth:refresh:{token}";
    private static string UserKey(Guid userId) => $"auth:user:{userId}:refresh";

    public async Task StoreRefreshToken(Guid userId, string refreshToken)
    {
        var existing = await db.StringGetAsync(UserKey(userId));
        if (existing.HasValue)
            await db.KeyDeleteAsync(TokenKey(existing!));

        var batch = db.CreateBatch();
        var setToken = batch.StringSetAsync(TokenKey(refreshToken), userId.ToString(), ttl);
        var setUser = batch.StringSetAsync(UserKey(userId), refreshToken, ttl);
        batch.Execute();
        await Task.WhenAll(setToken, setUser);
    }

    public async Task<bool> IsRefreshTokenValid(string refreshToken)
    {
        var userIdValue = await db.StringGetAsync(TokenKey(refreshToken));
        if (!userIdValue.HasValue) return false;
        if (!Guid.TryParse((string?)userIdValue, out var userId)) return false;

        var active = await db.StringGetAsync(UserKey(userId));
        return active.HasValue && active == refreshToken;
    }

    public async Task DeleteRefreshToken(string refreshToken)
    {
        var userIdValue = await db.StringGetAsync(TokenKey(refreshToken));
        await db.KeyDeleteAsync(TokenKey(refreshToken));

        if (!userIdValue.HasValue || !Guid.TryParse((string?)userIdValue, out var userId)) return;

        // Only clear the user→token pointer if it still points to this token —
        // otherwise a fresh login that arrived in the meantime would be wiped.
        var current = await db.StringGetAsync(UserKey(userId));
        if (current.HasValue && current == refreshToken)
            await db.KeyDeleteAsync(UserKey(userId));
    }

    public async Task<bool> TryRefreshToken(string oldRefreshToken, string newRefreshToken)
    {
        if (!await IsRefreshTokenValid(oldRefreshToken)) return false;

        var userIdValue = await db.StringGetAsync(TokenKey(oldRefreshToken));
        if (!userIdValue.HasValue || !Guid.TryParse((string?)userIdValue, out var userId)) return false;

        await DeleteRefreshToken(oldRefreshToken);
        await StoreRefreshToken(userId, newRefreshToken);
        return true;
    }

    public async Task<Guid> GetUserIdByTokenAsync(string refreshToken)
    {
        var value = await db.StringGetAsync(TokenKey(refreshToken));
        return value.HasValue && Guid.TryParse((string?)value, out var id) ? id : Guid.Empty;
    }

    public async Task DeleteAllTokensForUserAsync(Guid userId)
    {
        var token = await db.StringGetAsync(UserKey(userId));
        await db.KeyDeleteAsync(UserKey(userId));
        if (token.HasValue)
            await db.KeyDeleteAsync(TokenKey(token!));
    }
}
