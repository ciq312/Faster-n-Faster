using System.Security.Cryptography;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.UseCases.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FasterNFaster.Tests.Services;

public class JwtTokenFactoryTests
{
    private readonly JwtOptions jwtOptions = new()
    {
        JWT_PRIVATE_TOKEN = Convert.ToBase64String(RSA.Create(2048).ExportRSAPrivateKey()),
        Issuer = "issuer",
        Audience = "audience",
        AccessTokenLifetime = TimeSpan.FromMinutes(5),
        GuestAccessTokenLifetime = TimeSpan.FromDays(7)
    };

    private JwtTokenFactory CreateFactory() => new(Options.Create(jwtOptions));

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
