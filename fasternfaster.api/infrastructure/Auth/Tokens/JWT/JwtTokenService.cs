using FasterNFaster.Api.UseCases.Auth.Tokens;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class JwtTokenService(
    IJwtTokenFactory jwtTokenFactory,
    IRefreshTokenRepository tokenStore,
    IUserRepository repo,
    IOptions<JwtOptions> jwtOptions) : ITokenService
{
    private readonly TimeSpan refreshTokenLifetime = jwtOptions.Value.RefreshTokenLifetime;
    private readonly bool slidingExpiration = jwtOptions.Value.SlidingRefreshExpiration;

    public async Task<TokenPair> IssuePlayerTokens(Guid userId, string userName)
    {
        var accessToken = jwtTokenFactory.CreateAccessToken(userId.ToString(), userName);
        var refreshToken = jwtTokenFactory.CreateRefreshToken();

        await tokenStore.Issue(userId, refreshToken, refreshTokenLifetime);

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

        var ttl = slidingExpiration ? refreshTokenLifetime : (TimeSpan?)null;

        var userId = await tokenStore.RotateRefreshToken(oldRefreshToken, newRefreshToken, ttl);
        if (userId is null) return null;

        var user = await repo.GetByIdAsync(userId.Value);
        if (user is null) return null;

        var newAccessToken = jwtTokenFactory.CreateAccessToken(userId.Value.ToString(), user.Nick);

        return new TokenPair(newAccessToken, newRefreshToken);
    }
}
