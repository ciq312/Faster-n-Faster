using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.UseCases.Auth.Ports;
using Microsoft.AspNetCore.Identity;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class PasswordHelper(IPasswordHasher<User> passwordHasher) : IPasswordHelper
{
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
