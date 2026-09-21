using System.Buffers.Text;
using System.ComponentModel;
using System.Security.Cryptography;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class ConfirmTokenFactory(
    IOptions<VerifyEmailOptions> verifyOptions,
    IOptions<ResetPasswordOptions> resetOptions
    ) : IConfirmTokenFactory
{
    private readonly VerifyEmailOptions verifyEmailOptions = verifyOptions.Value;
    private readonly ResetPasswordOptions resetPasswordOptions = resetOptions.Value;

    public Token GetToken(Guid userId, TokenType type)
    {
        TimeSpan expirationTime;
        switch (type)
        {
            case TokenType.EmailVerification:
                expirationTime = verifyEmailOptions.ExpirationTime;
                break;
            case TokenType.PasswordReset:
                expirationTime = resetPasswordOptions.ExpirationTime;
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