using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

namespace FasterNFaster.Api.Presentation.Web.Lobbies.GetLobbies;

public class GetLobbiesSummary : ApiSummary<GetLobbiesEndpoint>
{
    public GetLobbiesSummary()
    {
        Summary = "List public lobbies";
        Description = "Lists public lobbies newest first, `limit` per page (default 20, at most 100). Pass the previous page's `nextCursor` as `cursor` to get the next page; `nextCursor` is `null` on the last page. Private lobbies are never listed; find them with `GET /api/lobbies/by-invite/{inviteCode}`.";
        Response<GetLobbiesResult>(200, "One page of lobbies", example: new GetLobbiesResult(
            [
                new LobbyListItem(Guid.Parse("b7e6d5c4-3a2b-4c1d-9e8f-7a6b5c4d3e2f"), "Friday race", false, false, 3, 6, DateTimeOffset.Parse("2026-10-21T18:30:00+00:00"))
            ],
            "eyJDcmVhdGVkQXQiOiIyMDI2LTEwLTIxVDE4OjMwOjAwKzAwOjAwIiwiSWQiOiJiN2U2ZDVjNC0zYTJiLTRjMWQtOWU4Zi03YTZiNWM0ZDNlMmYifQ"));
        Response<ApiErrorResponse>(400, "`limit` isn't a number (`validation_failed`), or `cursor` wasn't issued by this endpoint (`invalid_cursor`)");
    }
}
