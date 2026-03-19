using FastEndpoints;
using FasterNFaster.Api.UseCases.Helpers;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Queries;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Results;

namespace FasterNFaster.Api.Web.Endpoints;

public class GetLobbiesEndpoint : EndpointWithoutRequest<GetLobbiesResult>
{
    private readonly IHandler<GetLobbiesQuery, GetLobbiesResult> _handler;

    public GetLobbiesEndpoint(IHandler<GetLobbiesQuery, GetLobbiesResult> handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/lobbies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Log.Information("Getting lobbies");
        var response = await _handler.Handle(new GetLobbiesQuery());
        Log.Information("sending lobbies");
        await Send.OkAsync(response, ct);
    }
}
