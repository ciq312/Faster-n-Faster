using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FasterNFaster.Api.Infrastructure;

public class PasswordHelper(IPasswordHasher<User> passwordHasher) : IPasswordHelper
{
    private readonly IPasswordHasher<User> passwordHasher = passwordHasher;

    public string HashPassword(User user, string password)
    {
        return passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string hashedPassword, string password)
    {
        var result = passwordHasher.VerifyHashedPassword(user, hashedPassword, password);
        return result == PasswordVerificationResult.Success;
    }
}
