using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Users.LoginUsers;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Handlers;

public class LoginUserHandlerTests
{
    private static LoginUserHandler CreateHandler(FakeUserRepository repo) =>
        new(repo, PasswordHelperFactory.Create(), new FakeTokenService());

    [Fact]
    public async Task LoginNotFound_ShouldThrowInvalidCredentials()
    {
        var repo = new FakeUserRepository();
        var handler = CreateHandler(repo);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => handler.Handle(new LoginUserCommand("noone", "pass123"), CancellationToken.None)
        );
    }

    [Fact]
    public async Task WrongPassword_ShouldThrowInvalidCredentials()
    {
        var repo = new FakeUserRepository();
        repo.Seed(new User("Player1", "mylogin", "correctpass"));
        var handler = CreateHandler(repo);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => handler.Handle(new LoginUserCommand("mylogin", "wrongpass"), CancellationToken.None)
        );
    }

    [Fact]
    public async Task CorrectCredentials_ShouldReturnTokenAndId()
    {
        var repo = new FakeUserRepository();
        var user = new User("Player1", "mylogin", "pass123");
        user.SetEmailVerified();
        repo.Seed(user);

        var handler = CreateHandler(repo);

        var result = await handler.Handle(new LoginUserCommand("mylogin", "pass123"), CancellationToken.None);

        Assert.Equal(user.Id, result.UserId);
    }
}
