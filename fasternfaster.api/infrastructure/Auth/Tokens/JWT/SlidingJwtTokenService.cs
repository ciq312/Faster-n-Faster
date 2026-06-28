using FasterNFaster.Api.UseCases.Auth.Tokens;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class SlidingJwtTokenService(
    IJwtTokenFactory jwtTokenFactory,
    IRefreshTokenRepository tokenStore,
    IUserRepository repo) : ITokenService
{
    public async Task<TokenPair> IssuePlayerTokens(Guid userId, string userName)
    {
        var accessToken = jwtTokenFactory.CreateAccessToken(userId.ToString(), userName);
        var refreshToken = jwtTokenFactory.CreateRefreshToken();

        await tokenStore.StoreRefreshToken(userId, refreshToken);

        return new TokenPair(accessToken, refreshToken);
    }

    public TokenPair IssueGuestTokens(Guid guestId, string guestName)
    {
        var guestAccessToken = jwtTokenFactory.CreateGuestAccessToken(guestId.ToString(), guestName);
        return new TokenPair(guestAccessToken, null);
    }

    public async Task<TokenPair?> TryRefreshTokens(string oldRefreshToken)
    {
        var newRefreshToken = jwtTokenFactory.CreateRefreshToken();

        var userId = await tokenStore.GetUserIdByTokenAsync(oldRefreshToken);
        if (userId == default) return null;

        var user = await repo.GetByIdAsync(userId);
        if (user == null) return null;

        if (!await tokenStore.TryRefreshToken(oldRefreshToken, newRefreshToken)) return null;

        var newAccessToken = jwtTokenFactory.CreateAccessToken(userId.ToString(), user.Nick);
        await tokenStore.StoreRefreshToken(userId, newRefreshToken);

        return new TokenPair(newAccessToken, newRefreshToken);
    }
}
