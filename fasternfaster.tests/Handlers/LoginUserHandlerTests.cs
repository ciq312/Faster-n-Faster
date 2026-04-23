using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Users.LoginUsers;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Handlers;

public class LoginUserHandlerTests
{
    [Fact]
    public async Task LoginNotFound_ShouldThrowKeyNotFound()
    {
        var repo = new FakeUserRepository();
        var handler = new LoginUserHandler(repo, PasswordHelperFactory.Create());

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => handler.Handle(new LoginUserCommand("noone", "pass123"))
        );
    }

    [Fact]
    public async Task WrongPassword_ShouldThrowInvalidData()
    {
        var repo = new FakeUserRepository();
        repo.Seed(new User("Player1", "mylogin", "correctpass"));
        var handler = new LoginUserHandler(repo, PasswordHelperFactory.Create());

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => handler.Handle(new LoginUserCommand("mylogin", "wrongpass"))
        );
    }

    [Fact]
    public async Task CorrectCredentials_ShouldReturnTokenAndId()
    {
        var repo = new FakeUserRepository();
        var user = new User("Player1", "mylogin", "pass123");
        user.SetEmailVerified();
        repo.Seed(user);
        var handler = new LoginUserHandler(repo, PasswordHelperFactory.Create());

        var result = await handler.Handle(new LoginUserCommand("mylogin", "pass123"));

        Assert.Equal(user.Id, result.UserId);
    }
}
