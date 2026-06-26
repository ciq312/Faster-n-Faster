using FastEndpoints;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Queries;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Results;
using MediatR;

namespace FasterNFaster.Api.Web.Endpoints;

public class GetLobbiesEndpoint(ISender sender) : EndpointWithoutRequest<GetLobbiesResult>
{
    public override void Configure()
    {
        Get("/api/lobbies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var response = await sender.Send(new GetLobbiesQuery(), ct);
        await Send.OkAsync(response, ct);
    }
}
