namespace FasterNFaster.Api.Core.Exceptions;

public class BadRequestException(string message) : StatusException(message)
{
    public override int StatusCode => 400;
}
