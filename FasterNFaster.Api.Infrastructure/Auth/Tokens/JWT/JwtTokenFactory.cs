using System.Security.Cryptography;
using FastEndpoints.Security;
using FasterNFaster.Api.UseCases.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class JwtTokenFactory(IOptions<JwtOptions> options) : IJwtTokenFactory
{
    private readonly JwtOptions jwtOptions = options.Value;

    public IssuedToken CreateAccessToken(string userId, string userName)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(jwtOptions.AccessTokenLifetime);
        var token = JwtBearer.CreateToken(o =>
        {
            o.SigningAlgorithm = SecurityAlgorithms.RsaSha256;
            o.SigningStyle = TokenSigningStyle.Asymmetric;
            o.SigningKey = jwtOptions.JWT_PRIVATE_TOKEN;
            o.ExpireAt = expiresAt;
            o.Issuer = jwtOptions.Issuer;
            o.Audience = jwtOptions.Audience;
            o.User.Roles.Add(jwtOptions.PlayerRole);
            o.User.Claims.Add(("sub", userId));
            o.User.Claims.Add(("name", userName));
        });

        return new IssuedToken(token, expiresAt, now);
    }

    public IssuedToken CreateGuestAccessToken(string guestId, string guestName)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(jwtOptions.GuestAccessTokenLifetime);
        var token = JwtBearer.CreateToken(o =>
        {
            o.SigningAlgorithm = SecurityAlgorithms.RsaSha256;
            o.SigningStyle = TokenSigningStyle.Asymmetric;
            o.SigningKey = jwtOptions.JWT_PRIVATE_TOKEN;
            o.ExpireAt = expiresAt;
            o.Issuer = jwtOptions.Issuer;
            o.Audience = jwtOptions.Audience;
            o.User.Roles.Add(jwtOptions.GuestRole);
            o.User.Claims.Add(("sub", guestId));
            o.User.Claims.Add(("name", guestName));
        });

        return new IssuedToken(token, expiresAt, now);
    }

    public IssuedToken CreateRefreshToken()
    {
        var now = DateTime.UtcNow;
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new IssuedToken(token, now.Add(jwtOptions.RefreshTokenLifetime), now);
    }
}
