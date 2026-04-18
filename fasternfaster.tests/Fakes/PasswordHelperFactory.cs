using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Helpers.Implementations;
using Microsoft.AspNetCore.Identity;

namespace FasterNFaster.Tests.Fakes;

public static class PasswordHelperFactory
{
    public static PasswordHelper Create()
    {
        var passwordHasher = new PasswordHasher<User>();
        return new PasswordHelper(passwordHasher);
    }
}