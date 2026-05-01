using FasterNFaster.Api.Core.Exceptions;

namespace FasterNFaster.Api.Infrastructure.Db.Tokens;

public class TokenNotFoundException : DomainException
{
    public string Token { get; private set; }
    public TokenNotFoundException(string token) : base($"token {token} wasn't found")
    {
        Token = token;
    }
}