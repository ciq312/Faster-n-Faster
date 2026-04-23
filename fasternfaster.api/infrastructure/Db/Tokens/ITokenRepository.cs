using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.Infrastructure.Db.Tokens;

public interface ITokenRepository
{
    Task<Token?> GetByValueAsync(string token);
    Task<Token?> GetLatestForUserAsync(Guid userId, TokenType type);
    Task Add(Token token);
    Task Remove(Token token);
    Task RemoveAllForUser(Guid userId, TokenType type);
    Task SaveChangesAsync();
}
