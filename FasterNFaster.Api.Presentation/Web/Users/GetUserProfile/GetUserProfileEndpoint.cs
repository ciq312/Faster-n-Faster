using FastEndpoints;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.UseCases.Users.Services;

namespace FasterNFaster.Api.Presentation.Web.Users.GetUserProfile;

public class GetUserProfileEndpoint(IUserProfileService profileService) : EndpointWithoutRequest<GetUserProfileResponse>
{
    public override void Configure()
    {
        Get("/api/users/me/profile");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId))
            throw new UnauthorizedException("Not authorized");

        var stats = await profileService.GetProfileAsync(userId);

        if (stats == null) throw new NotFoundException("No statistics for this player");

        await Send.OkAsync(new GetUserProfileResponse(
            stats.User.Nick, stats.Wins, stats.RacesTyped, stats.BestWPM, stats.AvgWPM, stats.BestAccuracy, stats.AvgAccuracy, stats.WordsTyped), ct);
    }
}
