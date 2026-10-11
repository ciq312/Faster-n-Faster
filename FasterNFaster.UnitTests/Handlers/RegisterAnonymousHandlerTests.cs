using FasterNFaster.Api.UseCases.Auth.RegisterAnonymous;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Handlers;

public class RegisterAnonymousHandlerTests
{
    [Fact]
    public async Task IssuesGuestTokens_ForAGeneratedGuestId()
    {
        var tokenService = new FakeTokenService();
        var handler = new RegisterAnonymousHandler(tokenService);

        var result = await handler.Handle(new RegisterAnonymousCommand("Speedy"), CancellationToken.None);

        Assert.Equal("Speedy", result.UserName);
        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.Equal(result.UserId, tokenService.IssuedForUserId);
        Assert.Equal("guest-access", result.Tokens.AccessToken.Value);
        Assert.Null(result.Tokens.RefreshToken);
    }
}
