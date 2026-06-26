using FastEndpoints;
using FasterNFaster.Api.UseCases.Leaderboards;
using MediatR;

namespace FasterNFaster.Api.Web.Leaderboards;

public class GetLeaderboardsEndpoint(ISender sender) : Endpoint<GetLeaderboardsRequest, GetLeaderboardResults>
{
    public override void Configure()
    {
        Post("/api/leaderboards");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetLeaderboardsRequest req, CancellationToken ct)
    {
        var response = await sender.Send(new GetLeaderboardCommand(req.Criteria, req.IsDescending, req.PlayersCount), ct);
        await Send.OkAsync(response, cancellation: ct);
    }
}
