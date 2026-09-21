using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.UseCases.Services.Users;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Services;

public class ConfirmTokenIssuerTests
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(15);
    private readonly Guid userId = Guid.NewGuid();
    private readonly FakeTokenRepo tokenRepo = new();
    private readonly ConfirmTokenIssuer issuer;

    public ConfirmTokenIssuerTests()
    {
        var tokenFactory = new ConfirmTokenFactory(
            Options.Create(new VerifyEmailOptions { ExpirationTime = TimeSpan.FromDays(1) }),
            Options.Create(new ResetPasswordOptions { ExpirationTime = TimeSpan.FromDays(1) }));
        issuer = new ConfirmTokenIssuer(tokenRepo, tokenFactory);
    }

    [Fact]
    public async Task NoPriorToken_IssuesAndStoresToken()
    {
        Token? token = await issuer.TryIssue(userId, TokenType.PasswordReset, Cooldown);

        Assert.NotNull(token);
        Assert.Equal(userId, token.UserId);
        Assert.Equal(TokenType.PasswordReset, token.Type);
        Assert.Same(token, Assert.Single(tokenRepo.tokens));
    }

    [Fact]
    public async Task WithinCooldown_ReturnsNullAndKeepsPriorToken()
    {
        Token? first = await issuer.TryIssue(userId, TokenType.PasswordReset, Cooldown);

        Token? second = await issuer.TryIssue(userId, TokenType.PasswordReset, Cooldown);

        Assert.Null(second);
        Assert.Same(first, Assert.Single(tokenRepo.tokens));
    }

    [Fact]
    public async Task AfterCooldown_ReplacesPriorToken()
    {
        Token? first = await issuer.TryIssue(userId, TokenType.PasswordReset, Cooldown);
        first!.CreatedAt = DateTime.UtcNow - Cooldown - TimeSpan.FromSeconds(1);

        Token? second = await issuer.TryIssue(userId, TokenType.PasswordReset, Cooldown);

        Assert.NotNull(second);
        Assert.NotEqual(first.Value, second.Value);
        Assert.Same(second, Assert.Single(tokenRepo.tokens));
    }

    [Fact]
    public async Task CooldownIsPerTokenType()
    {
        await issuer.TryIssue(userId, TokenType.PasswordReset, Cooldown);

        Token? verification = await issuer.TryIssue(userId, TokenType.EmailVerification, Cooldown);

        Assert.NotNull(verification);
        Assert.Equal(2, tokenRepo.tokens.Count);
    }
}
