using System.Security.Cryptography;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.UseCases.Auth.Ports;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FasterNFaster.UnitTests.Auth;

public class JwtTokenFactoryTests
{
    private readonly JwtOptions jwtOptions = new()
    {
        PrivateKey = Convert.ToBase64String(RSA.Create(2048).ExportRSAPrivateKey()),
        Issuer = "issuer",
        Audience = "audience",
        AccessTokenLifetime = TimeSpan.FromMinutes(5),
        GuestAccessTokenLifetime = TimeSpan.FromDays(7)
    };

    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 1, 1, 12, 34, 56, TimeSpan.Zero));

    private JwtTokenFactory CreateFactory() => new(Options.Create(jwtOptions), time);

    [Fact]
    public void CreateAccessToken_HasPlayerRoleClaimsAndLifetime()
    {
        var issued = CreateFactory().CreateAccessToken("user-1", "Alice");

        AssertToken(issued, "user-1", "Alice", jwtOptions.PlayerRole, jwtOptions.AccessTokenLifetime);
    }

    [Fact]
    public void CreateGuestAccessToken_HasGuestRoleClaimsAndLifetime()
    {
        var issued = CreateFactory().CreateGuestAccessToken("guest-1", "Guest");

        AssertToken(issued, "guest-1", "Guest", jwtOptions.GuestRole, jwtOptions.GuestAccessTokenLifetime);
    }

    [Fact]
    public void CreateAccessToken_ValidFromIssueTimeUntilLifetime()
    {
        var now = time.GetUtcNow();

        var issued = CreateFactory().CreateAccessToken("user-1", "Alice");

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(issued.Value);
        Assert.Equal(now.UtcDateTime, jwt.ValidFrom, TimeSpan.FromSeconds(1));
        Assert.Equal(now.UtcDateTime, jwt.IssuedAt, TimeSpan.FromSeconds(1));
        Assert.Equal(now.UtcDateTime + jwtOptions.AccessTokenLifetime, jwt.ValidTo, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateRefreshToken_ExpiresLifetimeAfterNow()
    {
        var now = time.GetUtcNow();

        var issued = CreateFactory().CreateRefreshToken();

        Assert.Equal(now, issued.CreatedAt);
        Assert.Equal(now + jwtOptions.RefreshTokenLifetime, issued.ExpiresAt);
    }

    private static void AssertToken(IssuedToken issued, string subject, string name, string role, TimeSpan lifetime)
    {
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(issued.Value);

        Assert.Equal(subject, jwt.GetClaim("sub").Value);
        Assert.Equal(name, jwt.GetClaim("name").Value);
        Assert.Equal(role, jwt.GetClaim("role").Value);
        Assert.Equal(lifetime, issued.ExpiresAt - issued.CreatedAt);
        Assert.Equal(issued.ExpiresAt, jwt.ValidTo, TimeSpan.FromSeconds(1));
    }
}
