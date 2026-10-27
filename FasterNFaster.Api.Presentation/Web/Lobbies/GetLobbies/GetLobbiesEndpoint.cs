using FastEndpoints;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;
using MediatR;

namespace FasterNFaster.Api.Presentation.Web.Lobbies.GetLobbies;

public class GetLobbiesEndpoint(ISender sender) : Endpoint<GetLobbiesRequest, GetLobbiesResult>
{
    public override void Configure()
    {
        Get("/api/lobbies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetLobbiesRequest req, CancellationToken ct)
    {
        var response = await sender.Send(new GetLobbiesQuery(req.Limit, req.Cursor), ct);
        await Send.OkAsync(response, ct);
    }
}
