using FasterNFaster.Api.Domain.Entities.Auth;

namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface IConfirmTokenFactory
{
    Token GetToken(Guid userId, TokenType type);
}
