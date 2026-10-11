using FasterNFaster.Api.Domain.Exceptions;

namespace FasterNFaster.Api.UseCases.Auth.Exceptions;

public class TokenNotFoundException : NotFoundException
{
    public TokenNotFoundException() : base($"token wasn't found or expired")
    {
    }
}
