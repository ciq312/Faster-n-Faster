namespace FasterNFaster.Api.Core.Exceptions;

public class ConflictException(string message) : StatusException(message)
{
    public override int StatusCode => 409;
}
