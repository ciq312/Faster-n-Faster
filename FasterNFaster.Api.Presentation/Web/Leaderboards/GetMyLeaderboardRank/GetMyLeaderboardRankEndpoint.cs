using FastEndpoints;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.UseCases.Leaderboards.GetLeaderboard;
using FasterNFaster.Api.UseCases.Leaderboards.GetMyLeaderboardRank;
using MediatR;

namespace FasterNFaster.Api.Presentation.Web.Leaderboards.GetMyLeaderboardRank;

public class GetMyLeaderboardRankEndpoint(ISender sender) : Endpoint<GetMyLeaderboardRankRequest, LeaderboardResultDTO>
{
    public override void Configure()
    {
        Get("/api/leaderboards/me");
        Roles("Player");
    }

    public override async Task HandleAsync(GetMyLeaderboardRankRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var playerId))
            throw new UnauthorizedException("Not authorized");

        var response = await sender.Send(new GetMyLeaderboardRankQuery(playerId, req.Sort, req.Descending), ct);
        await Send.OkAsync(response, cancellation: ct);
    }
}
