using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Options.JwtOptions;
using FasterNFaster.Api.Web.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Web.Services.Implementations;

public class TokenStore(IOptions<JwtOptions> options) : ITokenStore
{
    private readonly JwtOptions jwtOptions = options.Value;
    private readonly ConcurrentDictionary<string, DateTime> refreshTokenExpiries = new();
    private readonly ConcurrentDictionary<string, Guid> tokenToId = new();
    private readonly ConcurrentDictionary<Guid, string> idToToken = new();
    public Task StoreRefreshToken(Guid userId, string refreshToken)
    {
        if (idToToken.TryGetValue(userId, out var existing))
        {
            tokenToId.TryRemove(existing, out _);
            refreshTokenExpiries.TryRemove(existing, out _);
        }
        tokenToId[refreshToken] = userId;
        refreshTokenExpiries[refreshToken] = DateTime.UtcNow.Add(jwtOptions.RefreshTokenLifetime);
        idToToken[userId] = refreshToken;
        return Task.CompletedTask;
    }


    public async Task<bool> TryRefreshToken(string oldRefreshToken, string newRefreshToken)
    {
        if (await IsRefreshTokenValid(oldRefreshToken))
        {
            var userId = tokenToId[oldRefreshToken];

            await DeleteRefreshToken(oldRefreshToken);
            await StoreRefreshToken(userId, newRefreshToken);
            return true;

        }
        return false;
    }
    public Task<bool> IsRefreshTokenValid(string refreshToken)
    {
        if (!tokenToId.TryGetValue(refreshToken, out var userId))
            return Task.FromResult(false);

        var actualRefreshToken = idToToken.GetValueOrDefault(userId);
        if (actualRefreshToken != refreshToken)
            return Task.FromResult(false);

        var ExpiryTime = refreshTokenExpiries.GetValueOrDefault(refreshToken);
        if (ExpiryTime == default || ExpiryTime < DateTime.UtcNow) return Task.FromResult(false);
        return Task.FromResult(true);
    }

    public Task DeleteRefreshToken(string refreshToken)
    {
        if (tokenToId.TryRemove(refreshToken, out var userId))
        {
            refreshTokenExpiries.TryRemove(refreshToken, out _);
            idToToken.TryRemove(userId, out _);
        }
        return Task.CompletedTask;
    }

    public Task<Guid> GetUserIdByTokenAsync(string refreshToken)
    {
        return Task.FromResult(tokenToId.GetValueOrDefault(refreshToken));
    }

    public Task DeleteAllForUserAsync(Guid userId)
    {
        if (idToToken.TryRemove(userId, out var token))
        {
            tokenToId.TryRemove(token, out _);
            refreshTokenExpiries.TryRemove(token, out _);
        }
        return Task.CompletedTask;
    }
}

// register -> (id, token) -> client get (token) -> refresh -> token