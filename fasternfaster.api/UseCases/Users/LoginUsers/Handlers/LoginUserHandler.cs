using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.LoginUsers;

public class LoginUserHandler(IUserRepository repo, IPasswordHelper passwordHelper) : IRequestHandler<LoginUserCommand, LoginUserResult>
{
    private readonly IUserRepository userRepo = repo;
    private readonly IPasswordHelper passwordHelper = passwordHelper;

    public async Task<LoginUserResult> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        User user = await userRepo.GetUserByLoginAsync(command.Login)
            ?? throw new InvalidCredentialsException();

        if (!passwordHelper.VerifyPassword(user, user.Password!, command.Password))
            throw new InvalidCredentialsException();

        if (!user.IsEmailVerified) throw new EmailNotVerifiedException(user.Email ?? string.Empty);

        return new LoginUserResult(user.Id, user.Nick);
    }
}
