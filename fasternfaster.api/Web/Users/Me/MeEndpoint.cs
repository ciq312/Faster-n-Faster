using System.Security.Claims;
using FastEndpoints;

namespace FasterNFaster.Api.Web.Users.Me;

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

#if DEBUG
        Log.Information($"id : {userIdClaim}, name : {userName}");
#endif

        if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(userName))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(new MeResponse(Guid.Parse(userIdClaim), userName), ct);
    }
}

public record MeResponse(Guid UserId, string UserName);
