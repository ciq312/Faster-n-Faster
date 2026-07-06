using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.Infrastructure.Helpers;
using Microsoft.AspNetCore.Identity;

namespace FasterNFaster.Tests.Fakes;

public static class PasswordHelperFactory
{
    public static PasswordHelper Create()
    {

        return new PasswordHelper(new FakeHasher());
    }
}

public class FakeHasher : IPasswordHasher<User>
{
    public string HashPassword(User user, string password)
    {
        return password;
    }

    public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
    {
        if (hashedPassword == providedPassword) return PasswordVerificationResult.Success;
        else return PasswordVerificationResult.Failed;
    }
}