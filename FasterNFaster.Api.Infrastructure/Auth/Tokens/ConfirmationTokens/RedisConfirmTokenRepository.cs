using System.Text.Json;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using StackExchange.Redis;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class RedisConfirmTokenRepository(IConnectionMultiplexer redis) : IConfirmTokenRepository
{
    private readonly IDatabase db = redis.GetDatabase();

    private static string ValueKey(string value) => $"confirm:value:{value}";
    private static string UserKey(Guid userId, TokenType type) => $"confirm:user:{userId}:{type}";

    public async Task Add(Token token)
    {
        var ttl = token.ExpiresAt - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero) return;

        var userKey = UserKey(token.UserId, token.Type);
        var previous = await db.StringGetAsync(userKey);

        var tran = db.CreateTransaction();
        if (previous.HasValue)
            _ = tran.KeyDeleteAsync(ValueKey(previous!));
        _ = tran.StringSetAsync(ValueKey(token.Value), Serialize(token), ttl);
        _ = tran.StringSetAsync(userKey, token.Value, ttl);
        await tran.ExecuteAsync();
    }

    public async Task<Token?> GetByValueAsync(string token)
    {
        var json = await db.StringGetAsync(ValueKey(token));
        return json.HasValue ? Deserialize(json!) : null;
    }

    public async Task<Token?> GetLatestForUserAsync(Guid userId, TokenType type)
    {
        var value = await db.StringGetAsync(UserKey(userId, type));
        return value.HasValue ? await GetByValueAsync(value!) : null;
    }

    public async Task Remove(Token token)
    {
        await db.KeyDeleteAsync(ValueKey(token.Value));

        var userKey = UserKey(token.UserId, token.Type);
        var current = await db.StringGetAsync(userKey);
        if (current.HasValue && current == token.Value)
            await db.KeyDeleteAsync(userKey);
    }

    public async Task RemoveAllForUser(Guid userId, TokenType type)
    {
        var userKey = UserKey(userId, type);
        var value = await db.StringGetAsync(userKey);

        var tran = db.CreateTransaction();
        _ = tran.KeyDeleteAsync(userKey);
        if (value.HasValue)
            _ = tran.KeyDeleteAsync(ValueKey(value!));
        await tran.ExecuteAsync();
    }

    private static string Serialize(Token token) =>
        JsonSerializer.Serialize(new StoredToken(token.UserId, token.Value, token.Type, token.CreatedAt, token.ExpiresAt));

    private static Token Deserialize(string json)
    {
        var stored = JsonSerializer.Deserialize<StoredToken>(json)!;
        return new Token
        {
            UserId = stored.UserId,
            Value = stored.Value,
            Type = stored.Type,
            CreatedAt = stored.CreatedAt,
            ExpiresAt = stored.ExpiresAt
        };
    }

    private sealed record StoredToken(Guid UserId, string Value, TokenType Type, DateTime CreatedAt, DateTime ExpiresAt);
}
