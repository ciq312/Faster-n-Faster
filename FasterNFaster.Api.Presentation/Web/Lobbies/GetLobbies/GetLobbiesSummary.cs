using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

namespace FasterNFaster.Api.Presentation.Web.Lobbies.GetLobbies;

public class GetLobbiesSummary : ApiSummary<GetLobbiesEndpoint>
{
    public GetLobbiesSummary()
    {
        Summary = "List lobbies";
        Description = "Lists the lobbies, newest first. With `inviteCode`, returns only the lobby with that code, or an empty list if none matches.";
        Response<GetLobbiesResult>(200, "The lobbies", example: new GetLobbiesResult(
        [
            new LobbyListItem(Guid.Parse("b7e6d5c4-3a2b-4c1d-9e8f-7a6b5c4d3e2f"), "Friday race", false, false, 3, 6, DateTimeOffset.Parse("2026-10-21T18:30:00+00:00"))
        ]));
        RateLimited();
    }
}
