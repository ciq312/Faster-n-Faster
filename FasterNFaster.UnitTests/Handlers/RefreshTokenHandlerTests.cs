using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.UseCases.Auth;
using FasterNFaster.Api.UseCases.Users.RefreshToken;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Handlers;

public class RefreshTokenHandlerTests
{
    [Fact]
    public async Task ValidRefreshToken_ReturnsRotatedTokens()
    {
        var tokenService = new FakeTokenService
        {
            RefreshOutcome = new TokenPair(FakeTokenService.Token("new-access"), FakeTokenService.Token("new-refresh"))
        };
        var handler = new RefreshTokenHandler(tokenService);

        var result = await handler.Handle(new RefreshTokenCommand("old-refresh"), CancellationToken.None);

        Assert.Equal("new-access", result.Tokens.AccessToken.Value);
        Assert.Equal("new-refresh", result.Tokens.RefreshToken?.Value);
    }

    [Fact]
    public async Task InvalidRefreshToken_ThrowsUnauthorized()
    {
        var tokenService = new FakeTokenService { RefreshOutcome = null };
        var handler = new RefreshTokenHandler(tokenService);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new RefreshTokenCommand("expired-refresh"), CancellationToken.None));
    }
}
