using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.DTO;

namespace FasterNFaster.Api.UseCases.Users.RegisterUsers.Handlers;

public class RegisterUserHadnler : IHandler<RegisterUserCommand, RegisterUserResult>
{
    public IUserRepository _repo;

    public RegisterUserHadnler(IUserRepository repo)
    {
        _repo = repo;
    }
    public async Task<RegisterUserResult> Handle(RegisterUserCommand command)
    {
        if (await _repo.DoUserExistByLoginAsync(command.Login)) throw new InvalidOperationException($"user with login {command.Login} already exists");

        if (await _repo.DoUserExistByNickAsync(command.Nick)) throw new InvalidOperationException($"user with login {command.Nick} already exists");

        User user = new(command.Nick, command.Login, command.Password);

        Log.Information($"created user {user.Nick} {user.Login} {user.Password}");
        await _repo.AddAsync(user);

        return await Task.FromResult(new RegisterUserResult(user.Token, user.Id));
    }
}