using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure.Db.Tokens;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Api.UseCases.Users.ResetPassword;
using FasterNFaster.Api.Web.Options.JwtOptions;
using FasterNFaster.Api.Web.Services.Implementations;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Handlers;

public class ResetPasswordHandlerTests
{
    private const string KnownEmail = "test@gmail.com";
    private const string KnownLogin = "testlogin";
    private const string OldPassword = "testpass";
    private const string NewPassword = "newpass42";

    private class TestContext
    {
        public required FakeUserRepository UserRepo { get; init; }
        public required FakeTokenRepo TokenRepo { get; init; }
        public required TokenStore TokenStore { get; init; }
        public required InMemorySessionService Sessions { get; init; }
        public required ResetPasswordHandler Handler { get; init; }
        public required User User { get; init; }
        public required Token Token { get; init; }
    }

    private static async Task<TestContext> BuildWithValidResetToken()
    {
        var setup = await RegistredUsersSetup.Setup(
            new RegisterUserCommand("test", KnownLogin, KnownEmail, OldPassword));

        var user = await setup.repo.GetUserByLoginAsync(KnownLogin)
            ?? throw new InvalidOperationException("seeded user not found");

        // Discard the verification token created during registration.
        setup.TokenRepo.tokens.Clear();
        var resetToken = setup.TokenFactory.GetToken(user.Id, TokenType.PasswordReset);
        await setup.TokenRepo.Add(resetToken);

        var tokenStore = new TokenStore(Options.Create(new JwtOptions
        {
            RefreshTokenLifetime = TimeSpan.FromHours(1)
        }));
        var sessions = new InMemorySessionService(tokenStore);

        var handler = new ResetPasswordHandler(
            setup.repo, setup.TokenRepo, PasswordHelperFactory.Create(), sessions);

        return new TestContext
        {
            UserRepo = setup.repo,
            TokenRepo = setup.TokenRepo,
            TokenStore = tokenStore,
            Sessions = sessions,
            Handler = handler,
            User = user,
            Token = resetToken
        };
    }

    [Fact]
    public async Task ValidToken_UpdatesPasswordAndDeletesTokenAndInvalidatesSessions()
    {
        var ctx = await BuildWithValidResetToken();
        ctx.Sessions.SetUserSession(ctx.User.Id, "conn-1");
        await ctx.TokenStore.StoreRefreshToken(ctx.User.Id, "refresh-X");

        await ctx.Handler.Handle(new ResetPasswordCommand(ctx.Token.Value, NewPassword));

        // FakeHasher returns the raw password as its "hash", so asserting on Password works directly.
        Assert.Equal(NewPassword, ctx.User.Password);
        Assert.Empty(ctx.TokenRepo.tokens);
        Assert.Null(ctx.Sessions.GetActiveSession(ctx.User.Id));
        Assert.False(await ctx.TokenStore.IsRefreshTokenValid("refresh-X"));
    }

    [Fact]
    public async Task UnknownToken_Throws()
    {
        var ctx = await BuildWithValidResetToken();
        string originalPassword = ctx.User.Password!;

        await Assert.ThrowsAsync<TokenNotFoundException>(() =>
            ctx.Handler.Handle(new ResetPasswordCommand("not-a-real-token", NewPassword)));

        Assert.Equal(originalPassword, ctx.User.Password);
    }

    [Fact]
    public async Task ExpiredToken_ThrowsAndLeavesPasswordUnchanged()
    {
        var ctx = await BuildWithValidResetToken();
        ctx.Token.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        string originalPassword = ctx.User.Password!;

        await Assert.ThrowsAsync<TokenNotFoundException>(() =>
            ctx.Handler.Handle(new ResetPasswordCommand(ctx.Token.Value, NewPassword)));

        Assert.Equal(originalPassword, ctx.User.Password);
        // Token is deliberately not auto-purged on failed verify — operator / scheduled cleanup owns that.
        Assert.Single(ctx.TokenRepo.tokens);
    }

    [Fact]
    public async Task WrongTypeToken_ThrowsAndLeavesPasswordUnchanged()
    {
        var ctx = await BuildWithValidResetToken();
        // Replace the PasswordReset token with an EmailVerification token carrying the same value.
        ctx.TokenRepo.tokens.Clear();
        var wrongType = new Token
        {
            UserId = ctx.User.Id,
            Value = ctx.Token.Value,
            Type = TokenType.EmailVerification,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        await ctx.TokenRepo.Add(wrongType);
        string originalPassword = ctx.User.Password!;

        await Assert.ThrowsAsync<TokenNotFoundException>(() =>
            ctx.Handler.Handle(new ResetPasswordCommand(ctx.Token.Value, NewPassword)));

        Assert.Equal(originalPassword, ctx.User.Password);
    }

    [Fact]
    public async Task ReusedToken_Throws()
    {
        var ctx = await BuildWithValidResetToken();
        await ctx.Handler.Handle(new ResetPasswordCommand(ctx.Token.Value, NewPassword));

        await Assert.ThrowsAsync<TokenNotFoundException>(() =>
            ctx.Handler.Handle(new ResetPasswordCommand(ctx.Token.Value, "anotherpass")));
    }
}
