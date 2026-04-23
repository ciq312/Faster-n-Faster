using FasterNFaster.Api.Web.Options.JwtOptions;
using FasterNFaster.Api.Web.Services.Implementations;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Services;

public class InMemorySessionServiceTests
{
    private static (InMemorySessionService sessions, TokenStore tokenStore) Create()
    {
        var tokenStore = new TokenStore(Options.Create(new JwtOptions
        {
            RefreshTokenLifetime = TimeSpan.FromHours(1)
        }));
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
        await tokenStore.StoreRefreshToken(userId, token);

        await sessions.InvalidateAll(userId);

        Assert.False(await tokenStore.IsRefreshTokenValid(token));
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
        await tokenStore.StoreRefreshToken(userA, "refresh-A");
        await tokenStore.StoreRefreshToken(userB, "refresh-B");

        await sessions.InvalidateAll(userA);

        Assert.False(await tokenStore.IsRefreshTokenValid("refresh-A"));
        Assert.True(await tokenStore.IsRefreshTokenValid("refresh-B"));
    }
}
