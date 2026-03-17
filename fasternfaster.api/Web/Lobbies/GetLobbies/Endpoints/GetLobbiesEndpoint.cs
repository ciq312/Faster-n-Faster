using FastEndpoints;
using FasterNFaster.Api.UseCases.Helpers;
using FasterNFaster.Api.UseCases.Lobby.GetLobbies.Queries;
using FasterNFaster.Api.UseCases.Lobby.GetLobbies.Results;

namespace FasterNFaster.Api.Web.Endpoints;

public class GetLobbiesEndpoint : EndpointWithoutRequest<GetLobbiesResponse>
{
    private readonly IHandler<GetLobbiesQuery, GetLobbiesResponse> _handler;

    public GetLobbiesEndpoint(IHandler<GetLobbiesQuery, GetLobbiesResponse> handler)
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
        var response = await _handler.Handle(new GetLobbiesQuery());
        await Send.OkAsync(response, ct);
    }
}
