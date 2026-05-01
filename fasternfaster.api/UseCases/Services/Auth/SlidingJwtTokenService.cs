using System.Collections.Concurrent;
using System.Security.Cryptography;
using FastEndpoints.Security;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.Web.Services.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FasterNFaster.Api.Web.Services;

public class SlidingJwtTokenService(
    IJwtTokenFactory jwtTokenFactory,
    ITokenStore tokenStore,
    ICookiesWriter cookiesWriter,
    IUserRepository repo) : ITokenService
{
    private readonly IJwtTokenFactory jwtTokenFactory = jwtTokenFactory;
    private readonly ITokenStore tokenStore = tokenStore;
    private readonly ICookiesWriter cookiesWriter = cookiesWriter;
    private readonly IUserRepository repo = repo;

    public async Task HandlePlayerAuth(Guid userId, string userName)
    {
#if DEBUG
        Log.Information($"Handling player auth for userId: {userId}, userName: {userName}");
#endif
        var accessToken = jwtTokenFactory.CreateAccessToken(userId.ToString(), userName);
        var refreshToken = jwtTokenFactory.CreateRefreshToken();

        await tokenStore.StoreRefreshToken(userId, refreshToken);

        cookiesWriter.WriteAccessTokenCookie(accessToken);
        cookiesWriter.WriteRefreshTokenCookie(refreshToken);
    }

    public Task HandleGuestAuth(Guid guestId, string guestName)
    {
#if DEBUG
        Log.Information($"Handling guest auth for guestId: {guestId}, guestName: {guestName}");
#endif

        var guestAccessToken = jwtTokenFactory.CreateGuestAccessToken(guestId.ToString(), guestName);

        cookiesWriter.WriteGuestAccessTokenCookie(guestAccessToken);
        return Task.CompletedTask;
    }

    public async Task<bool> TryRefreshToken(string oldRefreshToken)
    {

        var newRefreshToken = jwtTokenFactory.CreateRefreshToken();

        var userId = await tokenStore.GetUserIdByTokenAsync(oldRefreshToken);
        if (userId == default) return false;

        var user = await repo.GetByIdAsync(userId);
        if (user == null) return false;

#if DEBUG
        Log.Information($"Refreshing token for userId: {userId}, userName: {user.Nick}");
#endif

        if (await tokenStore.TryRefreshToken(oldRefreshToken, newRefreshToken))
        {
            var newAccessToken = jwtTokenFactory.CreateAccessToken(userId.ToString(), user.Nick);

            await tokenStore.StoreRefreshToken(userId, newRefreshToken);

            cookiesWriter.WriteAccessTokenCookie(newAccessToken);
            cookiesWriter.WriteRefreshTokenCookie(newRefreshToken);
            return true;
        }

        return false;
    }

}

