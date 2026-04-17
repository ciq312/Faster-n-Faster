using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Factories.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FasterNFaster.Api.UseCases.Factories.Implementations;

public class UserFactory(IUserRepository repo) : IUserFactory
{
    private readonly IUserRepository repo = repo;

    public async Task<User> GetUser(Guid id, string nick, string role)
    {
        switch (role)
        {
            case "Guest":
                return User.Guest(id, nick);
            case "Player":
                return await repo.GetByIdAsync(id) ?? throw new UserNotFoundException(id);
            default:
                throw new ArgumentException($"Invalid role: {role}");
        }
    }
}

