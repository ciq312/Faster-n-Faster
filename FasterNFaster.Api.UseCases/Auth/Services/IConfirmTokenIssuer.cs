using FasterNFaster.Api.Domain.Entities.Auth;

namespace FasterNFaster.Api.UseCases.Auth.Services;

public interface IConfirmTokenIssuer
{
    Task<Token?> TryIssue(Guid userId, TokenType type);
}
