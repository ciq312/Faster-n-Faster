using FasterNFaster.Api.UseCases.Auth.Exceptions;
using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Users.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.LoginUser;

public class LoginUserHandler(IUserRepository userRepo, IPasswordHelper passwordHelper, ITokenService tokenService) : IRequestHandler<LoginUserCommand, LoginUserResult>
{
    public async Task<LoginUserResult> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetUserByLoginAsync(command.Login)
            ?? throw new InvalidCredentialsException();

        if (user.Password is null || !passwordHelper.VerifyPassword(user, user.Password, command.Password))
            throw new InvalidCredentialsException();

        if (!user.IsEmailVerified) throw new EmailNotVerifiedException(user.Email ?? string.Empty);

        var tokens = await tokenService.IssuePlayerTokens(user.Id, user.Nick);

        return new LoginUserResult(user.Id, user.Nick, tokens);
    }
}
