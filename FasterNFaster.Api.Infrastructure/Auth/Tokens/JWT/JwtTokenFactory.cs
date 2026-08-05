using System.Security.Cryptography;
using FastEndpoints.Security;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class JwtTokenFactory(IOptions<JwtOptions> options) : IJwtTokenFactory
{
    private readonly JwtOptions jwtOptions = options.Value;

    public string CreateAccessToken(string userId, string userName)
    {
        return JwtBearer.CreateToken(o =>
        {
            o.SigningAlgorithm = SecurityAlgorithms.RsaSha256;
            o.SigningStyle = TokenSigningStyle.Asymmetric;
            o.SigningKey = jwtOptions.JWT_PRIVATE_TOKEN;
            o.ExpireAt = DateTime.UtcNow.Add(jwtOptions.AccessTokenLifetime);
            o.Issuer = jwtOptions.Issuer;
            o.Audience = jwtOptions.Audience;
            o.User.Roles.Add(jwtOptions.PlayerRole);
            o.User.Claims.Add(("sub", userId));
            o.User.Claims.Add(("name", userName));
        });
    }

    public string CreateGuestAccessToken(string guestId, string guestName)
    {
        return JwtBearer.CreateToken(o =>
        {
            o.SigningAlgorithm = SecurityAlgorithms.RsaSha256;
            o.SigningStyle = TokenSigningStyle.Asymmetric;
            o.SigningKey = jwtOptions.JWT_PRIVATE_TOKEN;
            o.ExpireAt = DateTime.UtcNow.Add(jwtOptions.GuestAccessTokenLifetime);
            o.Issuer = jwtOptions.Issuer;
            o.Audience = jwtOptions.Audience;
            o.User.Roles.Add(jwtOptions.GuestRole);
            o.User.Claims.Add(("sub", guestId));
            o.User.Claims.Add(("name", guestName));
        });
    }

    public string CreateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
