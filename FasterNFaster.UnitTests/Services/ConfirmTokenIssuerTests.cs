using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Services.Users;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Services;

public class ConfirmTokenIssuerTests
{
    private static readonly TimeSpan Cooldown = ConfirmTokenFactoryHelper.DefaultOptions.Value.PasswordReset.Cooldown;
    private readonly Guid userId = Guid.NewGuid();
    private readonly FakeTokenRepo tokenRepo = new();
    private readonly ConfirmTokenIssuer issuer;

    public ConfirmTokenIssuerTests()
    {
        issuer = new ConfirmTokenIssuer(tokenRepo, ConfirmTokenFactoryHelper.Create(), ConfirmTokenFactoryHelper.DefaultOptions);
    }

    [Fact]
    public async Task NoPriorToken_IssuesAndStoresToken()
    {
        Token? token = await issuer.TryIssue(userId, TokenType.PasswordReset);

        Assert.NotNull(token);
        Assert.Equal(userId, token.UserId);
        Assert.Equal(TokenType.PasswordReset, token.Type);
        Assert.Same(token, Assert.Single(tokenRepo.tokens));
    }

    [Fact]
    public async Task WithinCooldown_ReturnsNullAndKeepsPriorToken()
    {
        Token? first = await issuer.TryIssue(userId, TokenType.PasswordReset);

        Token? second = await issuer.TryIssue(userId, TokenType.PasswordReset);

        Assert.Null(second);
        Assert.Same(first, Assert.Single(tokenRepo.tokens));
    }

    [Fact]
    public async Task AfterCooldown_ReplacesPriorToken()
    {
        Token? first = await issuer.TryIssue(userId, TokenType.PasswordReset);
        first!.CreatedAt = DateTime.UtcNow - Cooldown - TimeSpan.FromSeconds(1);

        Token? second = await issuer.TryIssue(userId, TokenType.PasswordReset);

        Assert.NotNull(second);
        Assert.NotEqual(first.Value, second.Value);
        Assert.Same(second, Assert.Single(tokenRepo.tokens));
    }

    [Fact]
    public async Task CooldownIsPerTokenType()
    {
        await issuer.TryIssue(userId, TokenType.PasswordReset);

        Token? verification = await issuer.TryIssue(userId, TokenType.EmailVerification);

        Assert.NotNull(verification);
        Assert.Equal(2, tokenRepo.tokens.Count);
    }
}
