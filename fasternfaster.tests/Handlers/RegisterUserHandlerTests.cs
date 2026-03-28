using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Handlers;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Handlers;

public class RegisterUserHandlerTests
{
    [Fact]
    public async Task DuplicateLogin_ShouldThrow()
    {
        var repo = new FakeUserRepository();
        repo.Seed(new User("Existing", "taken", "pass123"));
        var handler = new RegisterUserHadnler(repo);

        await Assert.ThrowsAsync<DuplicateLoginException>(
            () => handler.Handle(new RegisterUserCommand("NewNick", "taken", "pass123"))
        );
    }

    [Fact]
    public async Task DuplicateNick_ShouldThrow()
    {
        var repo = new FakeUserRepository();
        repo.Seed(new User("TakenNick", "existinglogin", "pass123"));
        var handler = new RegisterUserHadnler(repo);

        await Assert.ThrowsAsync<DuplicateNickException>(
            () => handler.Handle(new RegisterUserCommand("TakenNick", "newlogin", "pass123"))
        );
    }

    [Fact]
    public async Task ValidNewUser_ShouldReturnTokenAndId()
    {
        var repo = new FakeUserRepository();
        var handler = new RegisterUserHadnler(repo);

        var result = await handler.Handle(new RegisterUserCommand("Player1", "mylogin", "pass123"));

        Assert.NotNull(result.Token);
        Assert.NotEqual(Guid.Empty, result.UserId);
    }
}
