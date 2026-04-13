using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.DTO;

namespace FasterNFaster.Api.UseCases.Users.RegisterUsers.Handlers;

public class RegisterUserHandler(IUserRepository repo) : IHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IUserRepository repo = repo;

    public async Task<RegisterUserResult> Handle(RegisterUserCommand command)
    {

        if (await repo.DoUserExistByNickAsync(command.Nick)) throw new DuplicateNickException(command.Nick);

        if (await repo.GetUserByLoginAsync(command.Login) != null) throw new DuplicateLoginException(command.Login);

        User user = new(command.Nick, command.Login, command.Password);

#if DEBUG
        Log.Information($"created user {user.Nick} {user.Login} {user.Password}");
#endif
        await repo.AddAsync(user);

        return await Task.FromResult(new RegisterUserResult(user.Token, user.Id));
    }
}