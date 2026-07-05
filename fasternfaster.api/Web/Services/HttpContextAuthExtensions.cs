using System.Security.Claims;
using FasterNFaster.Api.UseCases.Interfaces.Auth;

namespace FasterNFaster.Api.Web.Services;

public static class HttpContextAuthExtensions
{
    // If the incoming request already carries a valid JWT (e.g. a user switching accounts),
    // tear down everything tied to that previous identity before the new credentials are issued.
    // No-op when the request is anonymous or the subject claim is malformed.
    public static async Task InvalidatePreviousUserIfAuthenticated(
        this HttpContext httpContext,
        ISessionService sessions)
    {
        if (httpContext?.User?.Identity?.IsAuthenticated != true) return;

        var subjectClaim =
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");

        if (!Guid.TryParse(subjectClaim, out var previousUserId)) return;

        await sessions.InvalidateAll(previousUserId);
    }
}
