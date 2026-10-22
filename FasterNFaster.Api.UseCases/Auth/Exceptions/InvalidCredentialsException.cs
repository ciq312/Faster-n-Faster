using FasterNFaster.Api.Domain.Exceptions;

namespace FasterNFaster.Api.UseCases.Auth.Exceptions;

// intentionally vague message — don't reveal whether login or password was wrong
public class InvalidCredentialsException : UnauthorizedException
{
    public InvalidCredentialsException()
        : base("Invalid login or password") { }

    public override string Code => ErrorCodes.InvalidCredentials;
}
