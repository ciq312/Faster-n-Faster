namespace FasterNFaster.Api.UseCases.Auth.Ports;

public interface ITokenService
{
    Task<TokenPair> IssuePlayerTokens(Guid userId, string userName);

    IssuedToken IssueGuestTokens(Guid guestId, string guestName);

    // Returns the rotated token pair, or null if the refresh token is invalid/expired.
    Task<TokenPair?> TryRefreshTokens(string refreshToken);
}
