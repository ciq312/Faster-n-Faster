using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Auth;

public class InMemorySessionServiceTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    private static (InMemorySessionService sessions, FakeRefreshTokenRepository tokenStore) Create()
    {
        var tokenStore = new FakeRefreshTokenRepository();
        var sessions = new InMemorySessionService(tokenStore);
        return (sessions, tokenStore);
    }

    [Fact]
    public async Task RevokeAllSessions_RemovesActiveSession()
    {
        var (sessions, _) = Create();
        var userId = Guid.NewGuid();
        sessions.SetUserSession(userId, "conn-1");

        await sessions.RevokeAllSessions(userId);

        Assert.Null(sessions.GetActiveSession(userId));
    }

    [Fact]
    public async Task RevokeAllSessions_RevokesRefreshTokens()
    {
        var (sessions, tokenStore) = Create();
        var userId = Guid.NewGuid();
        var token = "refresh-A";
        await tokenStore.Issue(userId, token, Ttl);

        await sessions.RevokeAllSessions(userId);

        Assert.Null(await tokenStore.RotateRefreshToken(token, "new", Ttl));
    }

    [Fact]
    public async Task RevokeAllSessions_OtherUsersSessionsUnaffected()
    {
        var (sessions, _) = Create();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        sessions.SetUserSession(userA, "conn-A");
        sessions.SetUserSession(userB, "conn-B");

        await sessions.RevokeAllSessions(userA);

        Assert.Null(sessions.GetActiveSession(userA));
        Assert.Equal("conn-B", sessions.GetActiveSession(userB));
    }

    [Fact]
    public async Task RevokeAllSessions_OtherUsersRefreshTokensUnaffected()
    {
        var (sessions, tokenStore) = Create();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await tokenStore.Issue(userA, "refresh-A", Ttl);
        await tokenStore.Issue(userB, "refresh-B", Ttl);

        await sessions.RevokeAllSessions(userA);

        Assert.Null(await tokenStore.RotateRefreshToken("refresh-A", "a-new", Ttl));
        Assert.Equal(userB, await tokenStore.RotateRefreshToken("refresh-B", "b-new", Ttl));
    }

    [Fact]
    public void MatchingSession_RemovesEntry()
    {
        var (sessions, _) = Create();
        var userId = Guid.NewGuid();
        sessions.SetUserSession(userId, "conn-1");

        sessions.ClearSessionIfActive(userId, "conn-1");

        Assert.Null(sessions.GetActiveSession(userId));
    }

    [Fact]
    public void StaleSession_KeepsNewerEntry()
    {
        var (sessions, _) = Create();
        var userId = Guid.NewGuid();
        sessions.SetUserSession(userId, "conn-1");
        sessions.SetUserSession(userId, "conn-2");

        sessions.ClearSessionIfActive(userId, "conn-1");

        Assert.Equal("conn-2", sessions.GetActiveSession(userId));
    }

    [Fact]
    public void UnknownUser_DoesNothing()
    {
        var (sessions, _) = Create();
        var userId = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        sessions.SetUserSession(otherUser, "conn-other");

        sessions.ClearSessionIfActive(userId, "conn-1");

        Assert.Equal("conn-other", sessions.GetActiveSession(otherUser));
    }
}
