using FasterNFaster.Api.Domain.Entities.Auth;
using FasterNFaster.Api.UseCases.Auth.Ports;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.UseCases.Auth.Services;

public class ConfirmTokenIssuer(
    IConfirmTokenRepository tokenRepo,
    IConfirmTokenFactory tokenFactory,
    IOptions<ConfirmTokenOptions> options,
    TimeProvider timeProvider) : IConfirmTokenIssuer
{
    public async Task<Token?> TryIssue(Guid userId, TokenType type)
    {
        var cooldown = options.Value.For(type).Cooldown;
        var latest = await tokenRepo.GetLatestForUserAsync(userId, type);
        if (latest is not null && timeProvider.GetUtcNow() - latest.CreatedAt < cooldown) return null;

        var token = tokenFactory.GetToken(userId, type);
        await tokenRepo.Add(token);
        return token;
    }
}
