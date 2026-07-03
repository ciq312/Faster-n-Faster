using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.Infrastructure.Db.Tokens;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Handlers;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Operators;

namespace FasterNFaster.Tests;

public static class RegisteredUsersSetup
{
    public static async Task<SetupResult> Setup(params RegisterUserCommand[] commands)
    {
        var userRepo = new FakeUserRepository();
        var emailSender = new FakeEmailSender();
        var tokenRepo = new FakeTokenRepo();
        var tokenFactory = new ConfirmTokenFactory(
            Options.Create(new VerifyEmailOptions
            {
                ExpirationTime = TimeSpan.FromDays(1)
            }),
            Options.Create(new ResetPasswordOptions
            {
                ExpirationTime = TimeSpan.FromDays(1)
            })
        );

        var handler = new RegisterUserHandler(userRepo, PasswordHelperFactory.Create(), emailSender, tokenRepo, tokenFactory);

        foreach (var command in commands) await handler.Handle(command, CancellationToken.None);

        return new SetupResult(userRepo, emailSender, tokenRepo, tokenFactory);

    }
    public record SetupResult(FakeUserRepository repo, FakeEmailSender EmailSender, FakeTokenRepo TokenRepo, ConfirmTokenFactory TokenFactory);


}