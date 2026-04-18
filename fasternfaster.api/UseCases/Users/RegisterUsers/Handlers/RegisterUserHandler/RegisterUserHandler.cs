using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.DTO;
using Microsoft.AspNetCore.Identity;

namespace FasterNFaster.Api.UseCases.Users.RegisterUsers.Handlers;

public class RegisterUserHandler(IUserRepository repo, IPasswordHelper passwordHelper) : IHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IUserRepository repo = repo;
    private readonly IPasswordHelper passwordHelper = passwordHelper;

    public async Task<RegisterUserResult> Handle(RegisterUserCommand command)
    {
        if (await repo.GetUserByLoginAsync(command.Login) != null) throw new DuplicateLoginException(command.Login);

        User user = new(command.Nick, command.Login, command.Password);
        string hashedPassword = passwordHelper.HashPassword(user, command.Password);
        user.SetPassword(hashedPassword);

#if DEBUG
        Log.Information($"created user {user.Nick} {user.Login}");
#endif
        await repo.AddAsync(user);

        return await Task.FromResult(new RegisterUserResult(user.Id, user.Nick));
    }
}