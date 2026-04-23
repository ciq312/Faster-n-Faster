using System.Security.Claims;
using FasterNFaster.Api.Web.Options.JwtOptions;
using FasterNFaster.Api.Web.Services;
using FasterNFaster.Api.Web.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Services;

// Exercises the account-switching step used by LoginUserEndpoint and GoogleCallbackEndpoint.
// Uses the real InMemorySessionService + TokenStore so we assert on observable state
// (session cleared, refresh token invalidated) instead of "was the method called".
public class HttpContextAuthExtensionsTests
{
    private static InMemorySessionService CreateSessions()
    {
        var tokenStore = new TokenStore(Options.Create(new JwtOptions
        {
            RefreshTokenLifetime = TimeSpan.FromHours(1)
        }));
        return new InMemorySessionService(tokenStore);
    }

    private static HttpContext BuildAuthenticatedContext(Guid userId, string claimType = ClaimTypes.NameIdentifier)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(claimType, userId.ToString()) }, authenticationType: "jwt");
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
    }

    [Fact]
    public async Task AccountSwitchingViaEmailLogin_InvalidatesPreviousUser()
    {
        var previousUserId = Guid.NewGuid();
        var context = BuildAuthenticatedContext(previousUserId);
        var sessions = CreateSessions();
        sessions.SetUserSession(previousUserId, "conn-X");

        await context.InvalidatePreviousUserIfAuthenticated(sessions);

        Assert.Null(sessions.GetActiveSession(previousUserId));
    }

    [Fact]
    public async Task UnauthenticatedRequest_DoesNotInvalidate()
    {
        var userId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            // Default ClaimsPrincipal has an unauthenticated identity.
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        var sessions = CreateSessions();
        sessions.SetUserSession(userId, "conn-X");

        await context.InvalidatePreviousUserIfAuthenticated(sessions);

        Assert.Equal("conn-X", sessions.GetActiveSession(userId));
    }

    [Fact]
    public async Task SameUserReLogin_StillInvalidates()
    {
        // Spec requires invalidation whether or not oldUserId == newUserId.
        var userId = Guid.NewGuid();
        var context = BuildAuthenticatedContext(userId);
        var sessions = CreateSessions();
        sessions.SetUserSession(userId, "conn-X");

        await context.InvalidatePreviousUserIfAuthenticated(sessions);

        Assert.Null(sessions.GetActiveSession(userId));
    }

    [Fact]
    public async Task SubClaimFallback_IsRespected()
    {
        // JWT subjects can come in as "sub" rather than ClaimTypes.NameIdentifier depending on mapping.
        var previousUserId = Guid.NewGuid();
        var context = BuildAuthenticatedContext(previousUserId, claimType: "sub");
        var sessions = CreateSessions();
        sessions.SetUserSession(previousUserId, "conn-X");

        await context.InvalidatePreviousUserIfAuthenticated(sessions);

        Assert.Null(sessions.GetActiveSession(previousUserId));
    }

    [Fact]
    public async Task MalformedSubject_DoesNotInvalidate()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") }, authenticationType: "jwt");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var sessions = CreateSessions();
        var unrelatedUser = Guid.NewGuid();
        sessions.SetUserSession(unrelatedUser, "conn-X");

        await context.InvalidatePreviousUserIfAuthenticated(sessions);

        Assert.Equal("conn-X", sessions.GetActiveSession(unrelatedUser));
    }
}
