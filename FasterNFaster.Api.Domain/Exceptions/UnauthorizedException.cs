namespace FasterNFaster.Api.Domain.Exceptions;

public class UnauthorizedException(string message) : StatusException(message)
{
    public override int StatusCode => 401;
}
