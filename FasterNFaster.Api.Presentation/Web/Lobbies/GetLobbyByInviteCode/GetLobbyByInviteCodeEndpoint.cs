using FastEndpoints;
using FasterNFaster.Api.Presentation.Extensions;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbyByInviteCode;
using MediatR;

namespace FasterNFaster.Api.Presentation.Web.Lobbies.GetLobbyByInviteCode;

public class GetLobbyByInviteCodeEndpoint(ISender sender) : Endpoint<GetLobbyByInviteCodeRequest, LobbyListItem>
{
    public override void Configure()
    {
        Get("/api/lobbies/by-invite/{inviteCode}");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimitPolicies.Lookup));
    }

    public override async Task HandleAsync(GetLobbyByInviteCodeRequest req, CancellationToken ct)
    {
        var response = await sender.Send(new GetLobbyByInviteCodeQuery(req.InviteCode), ct);
        await Send.OkAsync(response, ct);
    }
}
