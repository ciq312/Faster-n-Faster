using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FasterNFaster.Api.UseCases.Users.LoginUsers;

public class LoginUserHandler(IUserRepository repo, IPasswordHelper passwordHelper) : IHandler<LoginUserCommand, LoginUserResult>
{
    private readonly IUserRepository userRepo = repo;
    private readonly IPasswordHelper passwordHelper = passwordHelper;

    public async Task<LoginUserResult> Handle(LoginUserCommand command)
    {
        User user = await userRepo.GetUserByLoginAsync(command.Login)
            ?? throw new InvalidCredentialsException();

        if (!passwordHelper.VerifyPassword(user, user.Password!, command.Password))
            throw new InvalidCredentialsException();

        return new LoginUserResult(user.Id);
    }
}