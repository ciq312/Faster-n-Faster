namespace FasterNFaster.Api.Domain.Exceptions;

public class UnauthorizedException(string message = "Authentication required") : StatusException(message)
{
    public override string Code => ErrorCodes.NotAuthenticated;
}
