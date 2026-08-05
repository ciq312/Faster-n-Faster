namespace FasterNFaster.Api.Core.Exceptions;

public class ForbiddenException(string message) : StatusException(message)
{
    public override int StatusCode => 403;
}
