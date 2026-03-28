namespace FasterNFaster.Api.UseCases.Exceptions;

// intentionally vague message — don't reveal whether login or password was wrong
public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Invalid login or password") { }
}
