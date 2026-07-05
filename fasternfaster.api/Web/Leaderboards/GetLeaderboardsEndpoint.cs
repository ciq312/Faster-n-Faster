using FastEndpoints;
using FasterNFaster.Api.UseCases.Leaderboards;
using MediatR;

namespace FasterNFaster.Api.Web.Leaderboards;

public class GetLeaderboardsEndpoint(ISender sender) : Endpoint<GetLeaderboardsRequest, GetLeaderboardResults>
{
    public override void Configure()
    {
        Get("/api/leaderboards");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetLeaderboardsRequest req, CancellationToken ct)
    {
        var response = await sender.Send(new GetLeaderboardCommand(req.Sort, req.Descending, req.Page, req.PageSize), ct);
        await Send.OkAsync(response, cancellation: ct);
    }
}
