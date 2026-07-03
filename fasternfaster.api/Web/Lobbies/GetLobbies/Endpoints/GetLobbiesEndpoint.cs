using FastEndpoints;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Queries;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Results;
using MediatR;

namespace FasterNFaster.Api.Web.Endpoints;

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
    }

    public override async Task HandleAsync(GetLobbiesRequest req, CancellationToken ct)
    {
        var response = await sender.Send(new GetLobbiesQuery(req.InviteCode), ct);
        await Send.OkAsync(response, ct);
    }
}
