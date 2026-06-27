using System.Buffers.Text;
using System.ComponentModel;
using System.Security.Cryptography;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using Google.Apis.Util;

namespace FasterNFaster.Api.Infrastructure.Db.Tokens;

public class TokenFactory : ITokenFactory
{
    public Token GetToken(Guid userId, TokenType type)
    {
        TimeSpan expirationTime;
        switch (type)
        {
            case TokenType.EmailVerification:
                expirationTime = TimeSpan.FromHours(1);
                break;
            case TokenType.PasswordReset:
                expirationTime = TimeSpan.FromMinutes(15);
                break;
            default:
                throw new InvalidEnumArgumentException();
        }
        return new Token()
        {
            UserId = userId,
            Value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32)),
            Type = type,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expirationTime),
        };
    }
}