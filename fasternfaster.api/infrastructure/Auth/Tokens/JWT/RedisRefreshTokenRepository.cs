using FasterNFaster.Api.UseCases.Interfaces.Auth;
using StackExchange.Redis;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class RedisRefreshTokenRepository(IConnectionMultiplexer redis) : IRefreshTokenRepository
{
    private readonly IConnectionMultiplexer redis = redis;
    private readonly IDatabase db = redis.GetDatabase();

    private static string TokenToUserKey(string token) => $"auth:refresh:{token}";
    private static string UserToTokenKey(Guid userId, string token) => $"auth:user:{userId}:token:{token}";

    public async Task Issue(Guid userId, string refreshToken, TimeSpan ttl)
    {
        var tran = db.CreateTransaction();
        QueueStoreToken(tran, userId, refreshToken, ttl);
        await tran.ExecuteAsync();
    }

    public async Task<Guid?> RotateRefreshToken(string oldRefreshToken, string newRefreshToken, TimeSpan? ttl)
    {
        var oldTokenKey = TokenToUserKey(oldRefreshToken);
        var userIdValue = await db.StringGetAsync(oldTokenKey);
        if (!IsUserIdFound(userIdValue)) return null;

        var userId = Guid.Parse(userIdValue.ToString());

        var newTtl = ttl ?? await db.KeyTimeToLiveAsync(oldTokenKey);
        if (newTtl is null || newTtl <= TimeSpan.Zero) return null;

        var tran = db.CreateTransaction();
        QueueDeleteToken(tran, userId, oldRefreshToken);
        QueueStoreToken(tran, userId, newRefreshToken, newTtl.Value);
        if (!await tran.ExecuteAsync()) return null;

        return userId;
    }

    public async Task Invalidate(string refreshToken)
    {
        var tokenKey = TokenToUserKey(refreshToken);
        var userIdValue = await db.StringGetAsync(tokenKey);

        if (!IsUserIdFound(userIdValue))
        {
            await db.KeyDeleteAsync(tokenKey);
            return;
        }

        var userId = Guid.Parse(userIdValue.ToString());

        var tran = db.CreateTransaction();
        QueueDeleteToken(tran, userId, refreshToken);
        await tran.ExecuteAsync();
    }

    public async Task InvalidateAll(Guid userId)
    {
        foreach (var endpoint in redis.GetEndPoints())
        {
            var server = redis.GetServer(endpoint);
            var userTokenKeys = server.Keys(pattern: $"auth:user:{userId}:token:*").ToArray();
            if (userTokenKeys.Length == 0) continue;

            var batch = db.CreateBatch();
            foreach (var key in userTokenKeys)
            {
                var token = key.ToString().Split(':').Last();
                _ = batch.KeyDeleteAsync(key);
                _ = batch.KeyDeleteAsync(TokenToUserKey(token));
            }
            batch.Execute();
        }
    }

    private static bool IsUserIdFound(RedisValue userIdValue) =>
        userIdValue.HasValue && Guid.TryParse((string?)userIdValue, out _);

    private static void QueueStoreToken(ITransaction tran, Guid userId, string token, TimeSpan ttl)
    {
        _ = tran.StringSetAsync(TokenToUserKey(token), userId.ToString(), ttl);
        _ = tran.StringSetAsync(UserToTokenKey(userId, token), "active", ttl);
    }

    private static void QueueDeleteToken(ITransaction tran, Guid userId, string token)
    {
        _ = tran.KeyDeleteAsync(TokenToUserKey(token));
        _ = tran.KeyDeleteAsync(UserToTokenKey(userId, token));
    }
}
