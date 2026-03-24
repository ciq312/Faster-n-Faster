using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Helpers;
using FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Results;

namespace FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Handlers;

public class RegisterAnonymousHandler : IHandler<RegisterAnonymousCommand, RegisterAnonymousResult>
{
    private readonly IUserRepository _repo;

    public RegisterAnonymousHandler(IUserRepository repo)
    {
        _repo = repo;
    }

    public Task<RegisterAnonymousResult> Handle(RegisterAnonymousCommand command)
    {
        var user = new User(command.Nick);

        _repo.AddAsync(user);

        Log.Information("Registered anonymous user {UserId} as {Nick}", user.Id, user.Nick);
        return Task.FromResult(new RegisterAnonymousResult(user.Token));
    }
}
