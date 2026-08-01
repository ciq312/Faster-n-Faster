using FasterNFaster.Api.Core.Entities.Auth;

namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface IConfirmTokenFactory
{
    Token GetToken(Guid userId, TokenType type);
}
