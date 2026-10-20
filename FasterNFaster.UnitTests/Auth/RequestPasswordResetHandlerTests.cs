using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Auth;
using FasterNFaster.Api.UseCases.Auth.RegisterUser;
using FasterNFaster.Api.UseCases.Auth.RequestPasswordReset;
using FasterNFaster.Api.UseCases.Auth.Services;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Auth;

public class RequestPasswordResetHandlerTests
{
    private const string KnownEmail = "test@gmail.com";
    private const string KnownLogin = "testlogin";

    private static async Task<(RequestPasswordResetHandler handler, RegisteredUsersSetup.SetupResult setup)> Build()
    {
        var setup = await RegisteredUsersSetup.Setup(
            new RegisterUserCommand("test", KnownLogin, KnownEmail, "testpass"));
        setup.EmailSender.Sent.Clear();
        setup.TokenRepo.tokens.Clear();

        var handler = new RequestPasswordResetHandler(
            setup.repo, new ConfirmTokenIssuer(setup.TokenRepo, setup.TokenFactory, ConfirmTokenFactoryHelper.DefaultOptions, setup.Time), setup.EmailSender);
        return (handler, setup);
    }

    [Fact]
    public async Task UnknownEmail_NoTokenNoEmail()
    {
        var (handler, setup) = await Build();

        await handler.Handle(new RequestPasswordResetCommand("ghost@nowhere.com"), CancellationToken.None);

        Assert.Empty(setup.TokenRepo.tokens);
        Assert.Empty(setup.EmailSender.SentPasswordResets);
    }

    [Fact]
    public async Task RegisteredUser_IssuesPasswordResetTokenAndSendsEmail()
    {
        var (handler, setup) = await Build();

        await handler.Handle(new RequestPasswordResetCommand(KnownEmail), CancellationToken.None);

        var token = Assert.Single(setup.TokenRepo.tokens);
        Assert.Equal(TokenType.PasswordReset, token.Type);

        var email = Assert.Single(setup.EmailSender.SentPasswordResets);
        Assert.Equal(KnownEmail, email.Email);
        Assert.Equal(token.Value, email.Token);
    }

    [Fact]
    public async Task GoogleOnlyUser_NoTokenNoEmail()
    {
        var userRepo = new FakeUserRepository();
        var tokenRepo = new FakeTokenRepo();
        var emailSender = new FakeEmailSender();
        var tokenFactory = ConfirmTokenFactoryHelper.Create(TimeProvider.System);

        var googleUser = new User("googleNick");
        googleUser.SetEmail("google@user.com");
        userRepo.Seed(googleUser);

        var handler = new RequestPasswordResetHandler(userRepo, new ConfirmTokenIssuer(tokenRepo, tokenFactory, ConfirmTokenFactoryHelper.DefaultOptions, TimeProvider.System), emailSender);

        await handler.Handle(new RequestPasswordResetCommand("google@user.com"), CancellationToken.None);

        Assert.Empty(tokenRepo.tokens);
        Assert.Empty(emailSender.SentPasswordResets);
    }

    [Fact]
    public async Task SecondRequestWithinCooldown_IsSilentNoOp()
    {
        var (handler, setup) = await Build();

        await handler.Handle(new RequestPasswordResetCommand(KnownEmail), CancellationToken.None);
        await handler.Handle(new RequestPasswordResetCommand(KnownEmail), CancellationToken.None);

        Assert.Single(setup.TokenRepo.tokens);
        Assert.Single(setup.EmailSender.SentPasswordResets);
    }

    [Fact]
    public async Task SecondRequestAfterCooldown_ReplacesPriorToken()
    {
        var (handler, setup) = await Build();

        await handler.Handle(new RequestPasswordResetCommand(KnownEmail), CancellationToken.None);
        setup.Time.Advance(ConfirmTokenFactoryHelper.DefaultOptions.Value.PasswordReset.Cooldown + TimeSpan.FromSeconds(1));

        await handler.Handle(new RequestPasswordResetCommand(KnownEmail), CancellationToken.None);

        Assert.Single(setup.TokenRepo.tokens);
        Assert.Equal(2, setup.EmailSender.SentPasswordResets.Count);
    }
}
