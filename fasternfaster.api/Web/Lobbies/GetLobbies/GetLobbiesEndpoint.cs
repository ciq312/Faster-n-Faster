using FastEndpoints;
using FasterNFaster.Api.Extensions;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;
using MediatR;

namespace FasterNFaster.Api.Web.GetLobbies;

public class GetLobbiesRequest
{
    [QueryParam]
    public string? InviteCode { get; set; }
}

public class GetLobbiesEndpoint(ISender sender) : Endpoint<GetLobbiesRequest, GetLobbiesResult>
{
    public override void Configure()
    {
        Get("/api/lobbies");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimitPolicies.Lookup));
    }

    public override async Task HandleAsync(GetLobbiesRequest req, CancellationToken ct)
    {
        var response = await sender.Send(new GetLobbiesQuery(req.InviteCode), ct);
        await Send.OkAsync(response, ct);
    }
}
