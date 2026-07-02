using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.LoginUsers;

public class LoginUserHandler(IUserRepository repo, IPasswordHelper passwordHelper, ITokenService tokenService) : IRequestHandler<LoginUserCommand, LoginUserResult>
{
    private readonly IUserRepository userRepo = repo;
    private readonly IPasswordHelper passwordHelper = passwordHelper;
    private readonly ITokenService tokenService = tokenService;

    public async Task<LoginUserResult> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        User user = await userRepo.GetUserByLoginAsync(command.Login)
            ?? throw new InvalidCredentialsException();

        if (!passwordHelper.VerifyPassword(user, user.Password!, command.Password))
            throw new InvalidCredentialsException();

        if (!user.IsEmailVerified) throw new EmailNotVerifiedException(user.Email ?? string.Empty);

        var tokens = await tokenService.IssuePlayerTokens(user.Id, user.Nick);

        return new LoginUserResult(user.Id, user.Nick, tokens);
    }
}
