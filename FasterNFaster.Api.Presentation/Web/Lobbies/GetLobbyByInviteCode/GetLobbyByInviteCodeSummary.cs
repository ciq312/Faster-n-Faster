using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

namespace FasterNFaster.Api.Presentation.Web.Lobbies.GetLobbyByInviteCode;

public class GetLobbyByInviteCodeSummary : ApiSummary<GetLobbyByInviteCodeEndpoint>
{
    public GetLobbyByInviteCodeSummary()
    {
        Summary = "Find a lobby by invite code";
        Description = "Returns the lobby with this invite code, public or private. The code is case-insensitive.";
        Response<LobbyListItem>(200, "The lobby", example: new LobbyListItem(
            Guid.Parse("b7e6d5c4-3a2b-4c1d-9e8f-7a6b5c4d3e2f"), "Friends only", true, false, 2, 6, DateTimeOffset.Parse("2026-10-21T18:30:00+00:00")));
        Response<ApiErrorResponse>(404, "No lobby has this invite code (`invalid_invite_code`)");
        RateLimited();
    }
}
