using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure.Db.Tokens;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Api.UseCases.Users.RequestPasswordReset;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Handlers;

public class RequestPasswordResetHandlerTests
{
    private const string KnownEmail = "test@gmail.com";
    private const string KnownLogin = "testlogin";

    private static async Task<(RequestPasswordResetHandler handler, RegistredUsersSetup.SetupResult setup)> Build()
    {
        var setup = await RegistredUsersSetup.Setup(
            new RegisterUserCommand("test", KnownLogin, KnownEmail, "testpass"));
        // Clear the verification-email-from-registration so we assert on reset calls only.
        setup.EmailSender.Sent.Clear();
        // Clear verification token for clean token-state assertions.
        setup.TokenRepo.tokens.Clear();

        var handler = new RequestPasswordResetHandler(
            setup.repo, setup.TokenRepo, setup.TokenFactory, setup.EmailSender);
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
        var tokenFactory = new TokenFactory();

        // Anonymous-ctor user has null Login and null Password — same shape as a Google-only account.
        var googleUser = new User("googleNick");
        googleUser.SetEmail("google@user.com");
        userRepo.Seed(googleUser);

        var handler = new RequestPasswordResetHandler(userRepo, tokenRepo, tokenFactory, emailSender);

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

        // Only one token, only one email — second call short-circuited.
        Assert.Single(setup.TokenRepo.tokens);
        Assert.Single(setup.EmailSender.SentPasswordResets);
    }

    [Fact]
    public async Task SecondRequestAfterCooldown_ReplacesPriorToken()
    {
        var (handler, setup) = await Build();

        await handler.Handle(new RequestPasswordResetCommand(KnownEmail), CancellationToken.None);
        // Simulate cooldown elapsed by backdating the first token's CreatedAt.
        setup.TokenRepo.tokens[0].CreatedAt = DateTime.UtcNow.AddSeconds(-30);

        await handler.Handle(new RequestPasswordResetCommand(KnownEmail), CancellationToken.None);

        // RemoveAllForUser(PasswordReset) runs before issuing the new token, so exactly one remains.
        Assert.Single(setup.TokenRepo.tokens);
        Assert.Equal(2, setup.EmailSender.SentPasswordResets.Count);
    }
}
