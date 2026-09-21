using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.UseCases.Services.Users;
using FasterNFaster.Api.UseCases.Users.RegisterUsers;
using FasterNFaster.Api.UseCases.Users.RequestPasswordReset;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Handlers;

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
            setup.repo, new ConfirmTokenIssuer(setup.TokenRepo, setup.TokenFactory), setup.EmailSender, new RequestPasswordResetOptions());
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

        var googleUser = new User("googleNick");
        googleUser.SetEmail("google@user.com");
        userRepo.Seed(googleUser);

        var handler = new RequestPasswordResetHandler(userRepo, new ConfirmTokenIssuer(tokenRepo, tokenFactory), emailSender, new RequestPasswordResetOptions());

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
        setup.TokenRepo.tokens[0].CreatedAt = DateTime.UtcNow.AddSeconds(-30);

        await handler.Handle(new RequestPasswordResetCommand(KnownEmail), CancellationToken.None);

        Assert.Single(setup.TokenRepo.tokens);
        Assert.Equal(2, setup.EmailSender.SentPasswordResets.Count);
    }
}
