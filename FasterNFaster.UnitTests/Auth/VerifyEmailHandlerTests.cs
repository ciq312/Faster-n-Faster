using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.UseCases.Auth.Exceptions;
using FasterNFaster.Api.UseCases.Auth.RegisterUser;
using FasterNFaster.Api.UseCases.Auth.VerifyEmail;
using FasterNFaster.UnitTests.Fakes;
using static FasterNFaster.UnitTests.RegisteredUsersSetup;

namespace FasterNFaster.UnitTests.Auth;

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
        var setup = await RegisteredUsersSetup.Setup(new RegisterUserCommand("test", "testlogin", "test@gmail.com", "testpass"));
        var userRepo = setup.repo;
        var user = await userRepo.GetUserByLoginAsync("testlogin") ?? throw new UserNotFoundException("login");
        Assert.False(user.IsEmailVerified);

        var tokenRepo = setup.TokenRepo;
        var token = tokenRepo.tokens[0].Value;

        var handler = new VerifyEmailHandler(userRepo, tokenRepo, new FakeUnitOfWork(), setup.Time);

        await Assert.ThrowsAsync<TokenNotFoundException>(async () =>
        {
            await handler.Handle(new VerifyEmailCommand("wrongTokenIHope"), CancellationToken.None);
        });
    }

    [Fact]
    public async Task ExpiredToken_ShouldThrow()
    {
        var setup = await RegisteredUsersSetup.Setup(new RegisterUserCommand("test", "testlogin", "test@gmail.com", "testpass"));
        var tokenVal = setup.TokenRepo.tokens[0].Value;
        setup.Time.Advance(ConfirmTokenFactoryHelper.DefaultOptions.Value.EmailVerification.ExpirationTime + TimeSpan.FromSeconds(1));
        var handler = new VerifyEmailHandler(setup.repo, setup.TokenRepo, new FakeUnitOfWork(), setup.Time);

        await Assert.ThrowsAsync<TokenNotFoundException>(() =>
            handler.Handle(new VerifyEmailCommand(tokenVal), CancellationToken.None));
    }

    private async Task<(User user, SetupResult setup)> PerformVerification()
    {
        var setup = await RegisteredUsersSetup.Setup(new RegisterUserCommand("test", "testlogin", "test@gmail.com", "testpass"));
        var userRepo = setup.repo;
        var user = await userRepo.GetUserByLoginAsync("testlogin") ?? throw new UserNotFoundException("login");
        Assert.False(user.IsEmailVerified);

        var tokenRepo = setup.TokenRepo;
        var tokenVal = tokenRepo.tokens[0].Value;

        var handler = new VerifyEmailHandler(userRepo, tokenRepo, new FakeUnitOfWork(), setup.Time);

        await handler.Handle(new VerifyEmailCommand(tokenVal), CancellationToken.None);

        return (user, setup);
    }
}
