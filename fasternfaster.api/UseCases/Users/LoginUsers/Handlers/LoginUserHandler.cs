using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Users.LoginUsers;

public class LoginUserHandler(IUserRepository repo) : IHandler<LoginUserCommand, LoginUserResult>
{
    private IUserRepository _userRepo = repo;

    public async Task<LoginUserResult> Handle(LoginUserCommand command)
    {
        User user = await _userRepo.GetUserByLoginAsync(command.Login) ?? throw new KeyNotFoundException($"user with login {command.Login} was not found");

        if (user.Password != command.Password) throw new InvalidDataException($"Password you entered is wrong");

        return new LoginUserResult(user.Token, user.Id);
    }
}