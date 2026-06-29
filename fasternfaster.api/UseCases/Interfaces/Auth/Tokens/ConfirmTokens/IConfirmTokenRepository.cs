using FasterNFaster.Api.Core.Entities.Auth;

namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface IConfirmTokenRepository
{
    Task<Token?> GetByValueAsync(string token);
    Task<Token?> GetLatestForUserAsync(Guid userId, TokenType type);
    Task Add(Token token);
    Task Remove(Token token);
    Task RemoveAllForUser(Guid userId, TokenType type);
}
