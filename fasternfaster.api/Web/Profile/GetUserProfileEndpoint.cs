using System.Security.Claims;
using FastEndpoints;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;


namespace FasterNFaster.Api.Web.Profile;

public class GetUserProfileEndpoint(AppDbContext appDbContext) : EndpointWithoutRequest<GetUserProfileResult>
{
    private readonly AppDbContext appDbContext = appDbContext;
    public override void Configure()
    {
        Get("api/users/me");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Guid.Parse(
            HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var stats = await appDbContext.Statistics.FindAsync(userId);

        if (stats == null) ThrowError("No statistics for this player", 404);

        await Send.OkAsync(new GetUserProfileResult(stats), ct);
    }
}

public record GetUserProfileResult(PlayerStatistics Stats);