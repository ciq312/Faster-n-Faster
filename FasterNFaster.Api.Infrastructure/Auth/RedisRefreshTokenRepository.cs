using System.Text.Json;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using StackExchange.Redis;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class RedisRefreshTokenRepository(IConnectionMultiplexer redis) : IRefreshTokenRepository
{
    private readonly IDatabase db = redis.GetDatabase();

    private static string TokenToUserKey(string token) => $"auth:refresh:{token}";
    private static string UserGenerationKey(Guid userId) => $"auth:user:{userId}:gen";

    public async Task Issue(Guid userId, string refreshToken, TimeSpan ttl)
    {
        var generation = await GetGeneration(userId);
        await db.StringSetAsync(TokenToUserKey(refreshToken), Serialize(userId, generation), ttl);
    }

    public async Task<Guid?> RotateRefreshToken(string oldRefreshToken, string newRefreshToken, TimeSpan ttl)
    {
        var tokenKey = TokenToUserKey(oldRefreshToken);
        var tokenValue = await db.StringGetAsync(tokenKey);
        if (!TryDeserialize(tokenValue, out var stored)) return null;

        var genKey = UserGenerationKey(stored.UserId);
        var currentGenerationValue = await db.StringGetAsync(genKey);
        if (stored.Generation != ParseGeneration(currentGenerationValue)) return null;

        var rotated = await TryCommitRotation(tokenKey, newRefreshToken, stored.UserId, genKey, currentGenerationValue, ttl);
        return rotated ? stored.UserId : null;
    }

    public Task Invalidate(string refreshToken) => db.KeyDeleteAsync(TokenToUserKey(refreshToken));

    public Task InvalidateAll(Guid userId) => db.StringIncrementAsync(UserGenerationKey(userId));

    private async Task<bool> TryCommitRotation(
        string oldTokenKey, string newRefreshToken, Guid userId, string genKey, RedisValue currentGenerationValue, TimeSpan ttl)
    {
        var tran = db.CreateTransaction();
        tran.AddCondition(Condition.KeyExists(oldTokenKey));
        tran.AddCondition(GenerationUnchanged(genKey, currentGenerationValue));
        _ = tran.KeyDeleteAsync(oldTokenKey);
        _ = tran.StringSetAsync(TokenToUserKey(newRefreshToken), Serialize(userId, ParseGeneration(currentGenerationValue)), ttl);
        return await tran.ExecuteAsync();
    }

    private async Task<long> GetGeneration(Guid userId) => ParseGeneration(await db.StringGetAsync(UserGenerationKey(userId)));

    private static long ParseGeneration(RedisValue value) => value.HasValue ? (long)value : 0;

    private static Condition GenerationUnchanged(string genKey, RedisValue genValue) =>
        genValue.HasValue ? Condition.StringEqual(genKey, genValue) : Condition.KeyNotExists(genKey);

    private static string Serialize(Guid userId, long generation) =>
        JsonSerializer.Serialize(new StoredToken(userId, generation));

    private static bool TryDeserialize(RedisValue value, out StoredToken token)
    {
        token = null!;
        if (!value.HasValue) return false;

        token = JsonSerializer.Deserialize<StoredToken>((string)value!)!;
        return token is not null;
    }

    private sealed record StoredToken(Guid UserId, long Generation);
}
