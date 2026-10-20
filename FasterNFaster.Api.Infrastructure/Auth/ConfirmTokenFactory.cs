using System.Buffers.Text;
using System.Security.Cryptography;
using FasterNFaster.Api.Domain.Entities.Auth;
using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Auth.Services;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class ConfirmTokenFactory(IOptions<ConfirmTokenOptions> options, TimeProvider timeProvider) : IConfirmTokenFactory
{
    public Token GetToken(Guid userId, TokenType type)
    {
        var expirationTime = options.Value.For(type).ExpirationTime;
        var now = timeProvider.GetUtcNow();
        return new Token()
        {
            UserId = userId,
            Value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32)),
            Type = type,
            CreatedAt = now,
            ExpiresAt = now.Add(expirationTime),
        };
    }
}
