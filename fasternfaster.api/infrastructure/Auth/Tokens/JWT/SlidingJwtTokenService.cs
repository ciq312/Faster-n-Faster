using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.Web.Services.Interfaces;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class SlidingJwtTokenService(
    IJwtTokenFactory jwtTokenFactory,
    IRefreshTokenRepository tokenStore,
    ICookiesWriter cookiesWriter,
    IUserRepository repo) : ITokenService
{
    public async Task HandlePlayerAuth(Guid userId, string userName)
    {
        var accessToken = jwtTokenFactory.CreateAccessToken(userId.ToString(), userName);
        var refreshToken = jwtTokenFactory.CreateRefreshToken();

        await tokenStore.StoreRefreshToken(userId, refreshToken);

        cookiesWriter.WriteAccessTokenCookie(accessToken);
        cookiesWriter.WriteRefreshTokenCookie(refreshToken);
    }

    public Task HandleGuestAuth(Guid guestId, string guestName)
    {
        cookiesWriter.DeleteTokensCookies();
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
