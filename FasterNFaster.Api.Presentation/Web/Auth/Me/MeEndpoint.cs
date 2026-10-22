using System.Security.Claims;
using FastEndpoints;
using FasterNFaster.Api.Domain.Exceptions;

namespace FasterNFaster.Api.Presentation.Web.Auth.Me;

public class MeEndpoint : EndpointWithoutRequest<MeResponse>
{
    public override void Configure()
    {
        Get("/api/auth/me");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue("sub");
        var userName = User.FindFirstValue("name");
        var role = User.FindFirstValue("role");

        if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(role))
        {
            throw new UnauthorizedException();
        }

        await Send.OkAsync(new MeResponse(Guid.Parse(userIdClaim), userName, role), ct);
    }
}

public record MeResponse(Guid UserId, string UserName, string Role);
