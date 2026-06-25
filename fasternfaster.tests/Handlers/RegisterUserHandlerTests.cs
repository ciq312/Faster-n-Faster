using System.Net.Http.Headers;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure.Db.Tokens;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Helpers.Implementations;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Handlers;
using FasterNFaster.Tests.Fakes;
using Microsoft.AspNetCore.Identity;

namespace FasterNFaster.Tests.Handlers;

public class RegisterUserHandlerTests
{

    [Fact]
    public async Task DuplicateLogin_ShouldThrow()
    {
        var repo = new FakeUserRepository();
        var emailSender = new FakeEmailSender();
        var tokenFactory = new TokenFactory();
        var tokenRepo = new FakeTokenRepo();

        repo.Seed(new User("Existing", "taken", "pass123"));

        var handler = new RegisterUserHandler(repo, PasswordHelperFactory.Create(), emailSender, tokenRepo, tokenFactory);

        await Assert.ThrowsAsync<DuplicateLoginException>(
            () => handler.Handle(new RegisterUserCommand("NewNick", "taken", "testemail@gmail.com", "pass123"), CancellationToken.None)
        );
    }

    [Fact]
    public async Task ValidNewUser_ShouldReturnNameAndId()
    {
        var repo = new FakeUserRepository();
        var emailSender = new FakeEmailSender();
        var tokenFactory = new TokenFactory();
        var tokenRepo = new FakeTokenRepo();

        var handler = new RegisterUserHandler(repo, PasswordHelperFactory.Create(), emailSender, tokenRepo, tokenFactory);

        var result = await handler.Handle(new RegisterUserCommand("Player1", "mylogin", "testemail@gmail.com", "pass123"), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.UserId);
    }

    [Fact]
    public async Task DuplicateEmail_ShouldThrow()
    {
        var repo = new FakeUserRepository();
        var emailSender = new FakeEmailSender();
        var tokenFactory = new TokenFactory();
        var tokenRepo = new FakeTokenRepo();

        var existing = new User("test1", "login1", "testpass");
        existing.SetEmail("test@gmail.com");
        repo.Seed(existing);

        var handler = new RegisterUserHandler(repo, PasswordHelperFactory.Create(), emailSender, tokenRepo, tokenFactory);

        await Assert.ThrowsAsync<DuplicateEmailException>(() => handler.Handle(new RegisterUserCommand("test2", "login2", "test@gmail.com", "testpass"), CancellationToken.None));
    }
    [Fact]
    public async Task SentEmail_ShouldBuild()
    {
        var repo = new FakeUserRepository();
        var emailSender = new FakeEmailSender();
        var tokenFactory = new TokenFactory();
        var tokenRepo = new FakeTokenRepo();
        var handler = new RegisterUserHandler(repo, PasswordHelperFactory.Create(), emailSender, tokenRepo, tokenFactory);

        await handler.Handle(new RegisterUserCommand("test2", "login2", "test@gmail.com", "testpass"), CancellationToken.None);

        Assert.Single(emailSender.Sent);
        Assert.Equal("test@gmail.com", emailSender.Sent[0].Email);
        Assert.NotEmpty(emailSender.Sent[0].Token);
    }

    [Fact]
    public async Task VerificationTokenCreated_ShouldStore()
    {
        var repo = new FakeUserRepository();
        var emailSender = new FakeEmailSender();
        var tokenFactory = new TokenFactory();
        var tokenRepo = new FakeTokenRepo();
        var handler = new RegisterUserHandler(repo, PasswordHelperFactory.Create(), emailSender, tokenRepo, tokenFactory);

        await handler.Handle(new RegisterUserCommand("test2", "login2", "test@gmail.com", "testpass"), CancellationToken.None);

        Assert.Single(tokenRepo.tokens);
        Assert.True(tokenRepo.tokens[0] is Token);
    }
}
