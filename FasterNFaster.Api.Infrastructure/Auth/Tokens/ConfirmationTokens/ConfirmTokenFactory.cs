using System.Buffers.Text;
using System.Security.Cryptography;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Services.Users;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class ConfirmTokenFactory(IOptions<ConfirmTokenOptions> options) : IConfirmTokenFactory
{
    public Token GetToken(Guid userId, TokenType type)
    {
        TimeSpan expirationTime = options.Value.For(type).ExpirationTime;
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
