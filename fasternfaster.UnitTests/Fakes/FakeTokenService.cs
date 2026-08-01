using FasterNFaster.Api.UseCases.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;

namespace FasterNFaster.Tests.Fakes;

public class FakeTokenService : ITokenService
{
    public Guid? IssuedForUserId { get; private set; }
    public TokenPair? RefreshOutcome { get; set; } = new("access", "refresh");

    public Task<TokenPair> IssuePlayerTokens(Guid userId, string userName)
    {
        IssuedForUserId = userId;
        return Task.FromResult(new TokenPair("access", "refresh"));
    }

    public TokenPair IssueGuestTokens(Guid guestId, string guestName)
    {
        IssuedForUserId = guestId;
        return new TokenPair("guest-access", null);
    }

    public Task<TokenPair?> TryRefreshTokens(string refreshToken) => Task.FromResult(RefreshOutcome);
}
