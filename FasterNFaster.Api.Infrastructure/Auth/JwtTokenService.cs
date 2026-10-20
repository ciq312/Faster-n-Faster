using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Users.Ports;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class JwtTokenService(
    IJwtTokenFactory jwtTokenFactory,
    IRefreshTokenRepository tokenStore,
    IUserRepository repo) : ITokenService
{
    public async Task<TokenPair> IssuePlayerTokens(Guid userId, string userName)
    {
        var accessToken = jwtTokenFactory.CreateAccessToken(userId.ToString(), userName);
        var refreshToken = jwtTokenFactory.CreateRefreshToken();

        await tokenStore.Issue(userId, refreshToken.Value, refreshToken.ExpiresAt - refreshToken.CreatedAt);

        return new TokenPair(accessToken, refreshToken);
    }

    public IssuedToken IssueGuestTokens(Guid guestId, string guestName) =>
        jwtTokenFactory.CreateGuestAccessToken(guestId.ToString(), guestName);

    public async Task<TokenPair?> TryRefreshTokens(string oldRefreshToken)
    {
        var newRefreshToken = jwtTokenFactory.CreateRefreshToken();

        var userId = await tokenStore.RotateRefreshToken(oldRefreshToken, newRefreshToken.Value, newRefreshToken.ExpiresAt - newRefreshToken.CreatedAt);
        if (userId is null) return null;

        var user = await repo.GetByIdAsync(userId.Value);
        if (user is null) return null;

        var newAccessToken = jwtTokenFactory.CreateAccessToken(userId.Value.ToString(), user.Nick);

        return new TokenPair(newAccessToken, newRefreshToken);
    }
}
