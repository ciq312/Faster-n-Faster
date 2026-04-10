using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Results;
using System.Data;

namespace FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Handlers;

public class RegisterAnonymousHandler(IUserRepository repo) : IHandler<RegisterAnonymousCommand, RegisterAnonymousResult>
{
    private readonly IUserRepository repo = repo;

    public async Task<RegisterAnonymousResult> Handle(RegisterAnonymousCommand command)
    {
        if (await repo.DoUserExistByNickAsync(command.Nick)) throw new DuplicateNameException($"User with nick {command.Nick} already exists");

        var user = new User(command.Nick);

        await repo.AddAsync(user);

#if DEBUG
        Log.Information("Registered anonymous user {UserId} as {Nick}", user.Id, user.Nick);
#endif
        return await Task.FromResult(new RegisterAnonymousResult(user.Token, user.Id));
    }
}
