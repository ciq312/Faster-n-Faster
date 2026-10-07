using FasterNFaster.Api.Core.Exceptions;

namespace FasterNFaster.Api.UseCases.Exceptions;

public class TokenNotFoundException : NotFoundException
{
    public TokenNotFoundException() : base($"token wasn't found or expired")
    {
    }
}