using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;

namespace FasterNFaster.Api.UseCases.Services.Users;

public class ConfirmTokenIssuer(
    IConfirmTokenRepository tokenRepo,
    IConfirmTokenFactory tokenFactory) : IConfirmTokenIssuer
{
    public async Task<Token?> TryIssue(Guid userId, TokenType type, TimeSpan cooldown)
    {
        Token? latest = await tokenRepo.GetLatestForUserAsync(userId, type);
        if (latest is not null && DateTime.UtcNow - latest.CreatedAt < cooldown) return null;

        Token token = tokenFactory.GetToken(userId, type);
        await tokenRepo.Add(token);
        return token;
    }
}
