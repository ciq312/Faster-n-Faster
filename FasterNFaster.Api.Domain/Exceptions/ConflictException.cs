namespace FasterNFaster.Api.Domain.Exceptions;

public class ConflictException(string message) : StatusException(message)
{
    public override int StatusCode => 409;
}
