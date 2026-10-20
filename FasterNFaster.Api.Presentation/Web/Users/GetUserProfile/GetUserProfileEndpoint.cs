using FastEndpoints;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.UseCases.Users.Services;

namespace FasterNFaster.Api.Presentation.Web.Users.GetUserProfile;

public class GetUserProfileEndpoint(IUserProfileService profileService) : EndpointWithoutRequest<GetUserProfileResult>
{
    public override void Configure()
    {
        Get("/api/users/me/profile");
        Description(x => x.Produces<ErrorResponse>(500));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId))
            throw new UnauthorizedException("Not authorized");

        var stats = await profileService.GetProfileAsync(userId);

        if (stats == null) throw new NotFoundException("No statistics for this player");

        await Send.OkAsync(new GetUserProfileResult(
            new UserStatisticsDTO(stats.User.Nick, stats.Wins, stats.RacesTyped, stats.BestWPM, stats.AvgWPM, stats.BestAccuracy, stats.AvgAccuracy, stats.WordsTyped)), ct);
    }
}

public record GetUserProfileResult(UserStatisticsDTO DTO);

public record UserStatisticsDTO(string Nick, int Wins, int RacesTyped, float BestWPM, float AvgWPM, float BestAccuracy, float AvgAccuracy, int WordsTyped);
