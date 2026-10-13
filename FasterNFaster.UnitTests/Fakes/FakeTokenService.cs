using FasterNFaster.Api.UseCases.Auth.Ports;

namespace FasterNFaster.UnitTests.Fakes;

public class FakeTokenService : ITokenService
{
    public Guid? IssuedForUserId { get; private set; }
    public TokenPair? RefreshOutcome { get; set; } = new(Token("access"), Token("refresh"));

    public static IssuedToken Token(string value) => new(value, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow);

    public Task<TokenPair> IssuePlayerTokens(Guid userId, string userName)
    {
        IssuedForUserId = userId;
        return Task.FromResult(new TokenPair(Token("access"), Token("refresh")));
    }

    public IssuedToken IssueGuestTokens(Guid guestId, string guestName)
    {
        IssuedForUserId = guestId;
        return Token("guest-access");
    }

    public Task<TokenPair?> TryRefreshTokens(string refreshToken) => Task.FromResult(RefreshOutcome);
}
