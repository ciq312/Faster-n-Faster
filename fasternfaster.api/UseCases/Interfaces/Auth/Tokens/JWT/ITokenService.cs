using FasterNFaster.Api.UseCases.Auth;

namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface ITokenService
{
    Task<TokenPair> IssuePlayerTokens(Guid userId, string userName);

    TokenPair IssueGuestTokens(Guid guestId, string guestName);

    // Returns the rotated token pair, or null if the refresh token is invalid/expired.
    Task<TokenPair?> TryRefreshTokens(string refreshToken);
}
