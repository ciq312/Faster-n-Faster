using FastEndpoints;
using FasterNFaster.Api.Infrastructure;

namespace FasterNFaster.Api.Web.Profile;

public class GetUserProfileEndpoint(IUserRepository repo, IStatisticsRepository statsRepo) : EndpointWithoutRequest<GetUserProfileResult>
{
    public override void Configure()
    {
        Get("api/users/profiles/me");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) ThrowError("Not authorized", 401);

        if (!await repo.IsUserRegistred(userId)) ThrowError("Not registered player", 401);

        var stats = await statsRepo.GetByUserIdAsync(userId);

        if (stats == null) ThrowError("No statistics for this player", 404);

        await Send.OkAsync(new GetUserProfileResult(
            new UserStatisticsDTO(stats!.User.Nick, stats.Wins, stats.RacesTyped, stats.BestWPM, stats.AvgWPM, stats.BestAccuracy, stats.AvgAccuracy, stats.WordsTyped)), ct);
    }
}

public record GetUserProfileResult(UserStatisticsDTO DTO);

public record UserStatisticsDTO(string Nick, int Wins, int RacesTyped, float BestWPM, float AvgWPM, float BestAccuracy, float AvgAccuracy, int WordsTyped);
