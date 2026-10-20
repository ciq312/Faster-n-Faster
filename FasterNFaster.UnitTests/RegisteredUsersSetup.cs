using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.UseCases.Auth.RegisterUser;
using FasterNFaster.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace FasterNFaster.UnitTests;

public static class RegisteredUsersSetup
{
    public static async Task<SetupResult> Setup(params RegisterUserCommand[] commands)
    {
        var userRepo = new FakeUserRepository();
        var emailSender = new FakeEmailSender();
        var tokenRepo = new FakeTokenRepo();
        var time = new FakeTimeProvider();
        var tokenFactory = ConfirmTokenFactoryHelper.Create(time);

        var handler = new RegisterUserHandler(userRepo, new FakeUnitOfWork(), PasswordHelperFactory.Create(), emailSender, tokenRepo, tokenFactory);

        foreach (var command in commands) await handler.Handle(command, CancellationToken.None);

        return new SetupResult(userRepo, emailSender, tokenRepo, tokenFactory, time);
    }

    public record SetupResult(FakeUserRepository repo, FakeEmailSender EmailSender, FakeTokenRepo TokenRepo, ConfirmTokenFactory TokenFactory, FakeTimeProvider Time);
}
