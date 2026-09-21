using System.Security.Cryptography;
using FasterNFaster.Api.UseCases.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class JwtTokenFactory(IOptions<JwtOptions> options) : IJwtTokenFactory
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    private readonly JwtOptions jwtOptions = options.Value;
    private readonly SigningCredentials signingCredentials = CreateSigningCredentials(options.Value.JWT_PRIVATE_TOKEN);

    public IssuedToken CreateAccessToken(string userId, string userName) =>
        CreateToken(userId, userName, jwtOptions.PlayerRole, jwtOptions.AccessTokenLifetime);

    public IssuedToken CreateGuestAccessToken(string guestId, string guestName) =>
        CreateToken(guestId, guestName, jwtOptions.GuestRole, jwtOptions.GuestAccessTokenLifetime);

    public IssuedToken CreateRefreshToken()
    {
        var now = DateTime.UtcNow;
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new IssuedToken(token, now.Add(jwtOptions.RefreshTokenLifetime), now);
    }

    private IssuedToken CreateToken(string subjectId, string name, string role, TimeSpan lifetime)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(lifetime);
        var token = TokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = signingCredentials,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subjectId,
                ["name"] = name,
                ["role"] = role
            }
        });

        return new IssuedToken(token, expiresAt, now);
    }

    private static SigningCredentials CreateSigningCredentials(string base64PrivateKey)
    {
        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(base64PrivateKey), out _);
        return new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
    }
}
