using System.Security.Claims;
using FastEndpoints;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Services.Interfaces;

namespace FasterNFaster.Api.Web.Users.Logout;

public class LogoutEndpoint(
    ISessionService sessions,
    ICookiesWriter cookies) : EndpointWithoutRequest
{
    private readonly ISessionService sessions = sessions;
    private readonly ICookiesWriter cookies = cookies;

    public override void Configure()
    {
        Post("/api/auth/logout");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue("sub");
        if (Guid.TryParse(userIdClaim, out var userId))
            await sessions.InvalidateAll(userId);

        cookies.DeleteTokensCookies();
        await Send.OkAsync(cancellation: ct);
    }
}
