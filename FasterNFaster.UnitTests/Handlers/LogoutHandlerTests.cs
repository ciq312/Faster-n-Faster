using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.UseCases.Users.Logout;

namespace FasterNFaster.Tests.Handlers;

public class LogoutHandlerTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    private static (LogoutHandler handler, InMemorySessionService sessions, InMemoryRefreshTokenRepository tokenStore) Create()
    {
        var tokenStore = new InMemoryRefreshTokenRepository();
        var sessions = new InMemorySessionService(tokenStore);
        return (new LogoutHandler(tokenStore, sessions), sessions, tokenStore);
    }

    [Fact]
    public async Task RefreshTokenOnly_InvalidatesThatToken()
    {
        var (handler, _, tokenStore) = Create();
        var userId = Guid.NewGuid();
        await tokenStore.Issue(userId, "refresh-A", Ttl);

        await handler.Handle(new LogoutCommand("refresh-A", null), CancellationToken.None);

        Assert.Null(await tokenStore.RotateRefreshToken("refresh-A", "new", Ttl));
    }

    [Fact]
    public async Task RefreshTokenOnly_LeavesOtherTokensOfSameUserValid()
    {
        var (handler, _, tokenStore) = Create();
        var userId = Guid.NewGuid();
        await tokenStore.Issue(userId, "refresh-A", Ttl);
        await tokenStore.Issue(userId, "refresh-B", Ttl);

        await handler.Handle(new LogoutCommand("refresh-A", null), CancellationToken.None);

        Assert.Equal(userId, await tokenStore.RotateRefreshToken("refresh-B", "b-new", Ttl));
    }

    [Fact]
    public async Task UserIdOnly_RevokesAllUserSessionsAndTokens()
    {
        var (handler, sessions, tokenStore) = Create();
        var userId = Guid.NewGuid();
        sessions.SetUserSession(userId, "conn-1");
        await tokenStore.Issue(userId, "refresh-A", Ttl);

        await handler.Handle(new LogoutCommand(null, userId), CancellationToken.None);

        Assert.Null(sessions.GetActiveSession(userId));
        Assert.Null(await tokenStore.RotateRefreshToken("refresh-A", "new", Ttl));
    }

    [Fact]
    public async Task RefreshTokenAndUserId_RevokesBoth()
    {
        var (handler, sessions, tokenStore) = Create();
        var userId = Guid.NewGuid();
        sessions.SetUserSession(userId, "conn-1");
        await tokenStore.Issue(userId, "refresh-A", Ttl);
        await tokenStore.Issue(userId, "refresh-B", Ttl);

        await handler.Handle(new LogoutCommand("refresh-A", userId), CancellationToken.None);

        Assert.Null(sessions.GetActiveSession(userId));
        Assert.Null(await tokenStore.RotateRefreshToken("refresh-A", "new", Ttl));
        Assert.Null(await tokenStore.RotateRefreshToken("refresh-B", "new", Ttl));
    }

    [Fact]
    public async Task NeitherRefreshTokenNorUserId_LeavesOtherUsersTokenValid()
    {
        var (handler, _, tokenStore) = Create();
        var otherUserId = Guid.NewGuid();
        await tokenStore.Issue(otherUserId, "refresh-other", Ttl);

        await handler.Handle(new LogoutCommand(null, null), CancellationToken.None);

        Assert.Equal(otherUserId, await tokenStore.RotateRefreshToken("refresh-other", "new", Ttl));
    }

    [Fact]
    public async Task UnknownRefreshToken_CompletesWithoutThrowing()
    {
        var (handler, _, _) = Create();

        await handler.Handle(new LogoutCommand("unknown-refresh", Guid.NewGuid()), CancellationToken.None);
    }
}
