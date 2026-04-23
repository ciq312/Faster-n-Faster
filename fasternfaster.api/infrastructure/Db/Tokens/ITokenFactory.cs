
using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.Infrastructure.Db.Tokens;

public interface ITokenFactory
{
    public Token GetToken(Guid userId, TokenType type);
}
