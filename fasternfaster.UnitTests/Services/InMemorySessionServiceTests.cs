using FasterNFaster.Api.Infrastructure.Auth;

namespace FasterNFaster.Tests.Services;

public class InMemorySessionServiceTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    private static (InMemorySessionService sessions, InMemoryRefreshTokenRepository tokenStore) Create()
    {
        var tokenStore = new InMemoryRefreshTokenRepository();
        var sessions = new InMemorySessionService(tokenStore);
        return (sessions, tokenStore);
    }

    [Fact]
    public async Task InvalidateAll_RemovesActiveSession()
    {
        var (sessions, _) = Create();
        var userId = Guid.NewGuid();
        sessions.SetUserSession(userId, "conn-1");

        await sessions.InvalidateAll(userId);

        Assert.Null(sessions.GetActiveSession(userId));
    }

    [Fact]
    public async Task InvalidateAll_InvalidatesRefreshToken()
    {
        var (sessions, tokenStore) = Create();
        var userId = Guid.NewGuid();
        var token = "refresh-A";
        await tokenStore.Issue(userId, token, Ttl);

        await sessions.InvalidateAll(userId);

        Assert.Null(await tokenStore.RotateRefreshToken(token, "new", Ttl));
    }

    [Fact]
    public async Task InvalidateAll_OtherUsersSessionsUnaffected()
    {
        var (sessions, _) = Create();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        sessions.SetUserSession(userA, "conn-A");
        sessions.SetUserSession(userB, "conn-B");

        await sessions.InvalidateAll(userA);

        Assert.Null(sessions.GetActiveSession(userA));
        Assert.Equal("conn-B", sessions.GetActiveSession(userB));
    }

    [Fact]
    public async Task InvalidateAll_OtherUsersRefreshTokensUnaffected()
    {
        var (sessions, tokenStore) = Create();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await tokenStore.Issue(userA, "refresh-A", Ttl);
        await tokenStore.Issue(userB, "refresh-B", Ttl);

        await sessions.InvalidateAll(userA);

        Assert.Null(await tokenStore.RotateRefreshToken("refresh-A", "a-new", Ttl));
        Assert.Equal(userB, await tokenStore.RotateRefreshToken("refresh-B", "b-new", Ttl));
    }
}
