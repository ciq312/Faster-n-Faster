using System.Security.Claims;
using FastEndpoints;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Services.Interfaces;

namespace FasterNFaster.Api.Web.Users.Logout;

public class LogoutEndpoint(
    ISessionService sessions,
    IAuthTokenWriter auth
    ) : EndpointWithoutRequest
{
    private readonly ISessionService sessions = sessions;
    private readonly IAuthTokenWriter auth = auth;

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

        auth.ClearAuth();
        await Send.OkAsync(cancellation: ct);
    }
}
