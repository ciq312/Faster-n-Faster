using FasterNFaster.Api.Domain.Entities.Auth;

namespace FasterNFaster.UnitTests.Domain;

public class TokenTests
{
    private static readonly DateTimeOffset ExpiresAt = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static Token CreateToken() => new()
    {
        UserId = Guid.NewGuid(),
        Value = "value",
        Type = TokenType.PasswordReset,
        CreatedAt = ExpiresAt.AddHours(-1),
        ExpiresAt = ExpiresAt,
    };

    [Fact]
    public void TokenBeforeExpiry_IsValid() =>
        Assert.True(CreateToken().IsValid(ExpiresAt.AddTicks(-1)));

    [Fact]
    public void TokenAtExpiry_IsNotValid() =>
        Assert.False(CreateToken().IsValid(ExpiresAt));
}
