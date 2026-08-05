using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Users.RegisterUsers;
using FasterNFaster.Api.UseCases.Users.ResendVerification;

namespace FasterNFaster.Tests.Handlers;

public class ResendVerificationHandlerTests
{
    private const string KnownEmail = "test@gmail.com";
    private const string KnownLogin = "testlogin";

    private static async Task<(ResendVerificationHandler handler, RegisteredUsersSetup.SetupResult setup)> Build()
    {
        var setup = await RegisteredUsersSetup.Setup(
            new RegisterUserCommand("test", KnownLogin, KnownEmail, "testpass"));
        // Drop the verification email + token created during registration for clean assertions.
        setup.EmailSender.Sent.Clear();
        setup.TokenRepo.tokens.Clear();

        var handler = new ResendVerificationHandler(
            setup.repo, setup.TokenRepo, setup.TokenFactory, setup.EmailSender, new ResendVerificationOptions());
        return (handler, setup);
    }

    [Fact]
    public async Task UnknownEmail_NoTokenNoEmail()
    {
        var (handler, setup) = await Build();

        await handler.Handle(new ResendVerificationCommand("ghost@nowhere.com"), CancellationToken.None);

        Assert.Empty(setup.TokenRepo.tokens);
        Assert.Empty(setup.EmailSender.Sent);
    }

    [Fact]
    public async Task UnverifiedUser_IssuesVerificationTokenAndSendsEmail()
    {
        var (handler, setup) = await Build();

        await handler.Handle(new ResendVerificationCommand(KnownEmail), CancellationToken.None);

        var token = Assert.Single(setup.TokenRepo.tokens);
        Assert.Equal(TokenType.EmailVerification, token.Type);

        var email = Assert.Single(setup.EmailSender.Sent);
        Assert.Equal(KnownEmail, email.Email);
        Assert.Equal(token.Value, email.Token);
    }

    [Fact]
    public async Task AlreadyVerifiedUser_NoTokenNoEmail()
    {
        var (handler, setup) = await Build();
        var user = setup.repo.Users.Single(u => u.Email == KnownEmail);
        user.SetEmailVerified();

        await handler.Handle(new ResendVerificationCommand(KnownEmail), CancellationToken.None);

        Assert.Empty(setup.TokenRepo.tokens);
        Assert.Empty(setup.EmailSender.Sent);
    }

    [Fact]
    public async Task SecondRequestWithinCooldown_IsSilentNoOp()
    {
        var (handler, setup) = await Build();

        await handler.Handle(new ResendVerificationCommand(KnownEmail), CancellationToken.None);
        await handler.Handle(new ResendVerificationCommand(KnownEmail), CancellationToken.None);

        Assert.Single(setup.TokenRepo.tokens);
        Assert.Single(setup.EmailSender.Sent);
    }

    [Fact]
    public async Task SecondRequestAfterCooldown_ReplacesPriorToken()
    {
        var (handler, setup) = await Build();

        await handler.Handle(new ResendVerificationCommand(KnownEmail), CancellationToken.None);
        setup.TokenRepo.tokens[0].CreatedAt = DateTime.UtcNow.AddSeconds(-30);

        await handler.Handle(new ResendVerificationCommand(KnownEmail), CancellationToken.None);

        Assert.Single(setup.TokenRepo.tokens);
        Assert.Equal(2, setup.EmailSender.Sent.Count);
    }
}
