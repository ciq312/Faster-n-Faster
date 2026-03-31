using FastEndpoints;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Leaderboards;

namespace FasterNFaster.Api.Web.Leaderboards;

public class GetLeaderboardsEndpoint(IHandler<GetLeaderboardCommand, GetLeaderboardResults> handler) : Endpoint<GetLeaderboardsRequest, GetLeaderboardResults>
{
    private readonly IHandler<GetLeaderboardCommand, GetLeaderboardResults> _handler = handler;
    public override void Configure()
    {
        Post("/api/leaderboards");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetLeaderboardsRequest req, CancellationToken ct)
    {
        var response = await _handler.Handle(new GetLeaderboardCommand(req.Criteria, req.IsDescending, req.PlayersCount));

        await Send.OkAsync(response, cancellation: ct);
    }

}
