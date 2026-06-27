using FasterNFaster.Api.Core.Entities.Auth;

namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface ITokenFactory
{
    Token GetToken(Guid userId, TokenType type);
}
