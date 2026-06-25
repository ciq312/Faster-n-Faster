using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure.Db.Tokens;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Api.UseCases.Users.VerifyEmail;
using static FasterNFaster.Tests.RegistredUsersSetup;

namespace FasterNFaster.Tests.Handlers;

public class VerifyEmailHandlerTests
{

    [Fact]
    public async Task CorrectToken_ShouldVerify()
    {
        var result = await PerformVerification();

        Assert.True(result.user.IsEmailVerified);
    }

    [Fact]
    public async Task IncorrectToken_ShouldThrow()
    {
        var setup = await RegistredUsersSetup.Setup(new RegisterUserCommand("test", "testlogin", "test@gmail.com", "testpass"));
        var userRepo = setup.repo;
        var user = await userRepo.GetUserByLoginAsync("testlogin") ?? throw new UserNotFoundException("login");
        Assert.False(user.IsEmailVerified);

        var tokenRepo = setup.TokenRepo;
        var token = tokenRepo.tokens[0].Value;

        var handler = new VerifyEmailHandler(userRepo, tokenRepo);

        await Assert.ThrowsAsync<TokenNotFoundException>(async () =>
        {
            await handler.Handle(new VerifyEmailCommand("wrongTokenIHope"), CancellationToken.None);
        });
    }

    private async Task<(User user, SetupResult setup)> PerformVerification()
    {
        var setup = await RegistredUsersSetup.Setup(new RegisterUserCommand("test", "testlogin", "test@gmail.com", "testpass"));
        var userRepo = setup.repo;
        var user = await userRepo.GetUserByLoginAsync("testlogin") ?? throw new UserNotFoundException("login");
        Assert.False(user.IsEmailVerified);

        var tokenRepo = setup.TokenRepo;
        var tokenVal = tokenRepo.tokens[0].Value;

        var handler = new VerifyEmailHandler(userRepo, tokenRepo);

        await handler.Handle(new VerifyEmailCommand(tokenVal), CancellationToken.None);

        return (user, setup);
    }
}