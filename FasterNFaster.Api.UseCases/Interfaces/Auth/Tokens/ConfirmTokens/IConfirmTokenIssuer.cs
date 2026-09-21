using FasterNFaster.Api.Core.Entities.Auth;

namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface IConfirmTokenIssuer
{
    Task<Token?> TryIssue(Guid userId, TokenType type, TimeSpan cooldown);
}
