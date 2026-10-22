using FasterNFaster.Api.Presentation.Web.Shared;

namespace FasterNFaster.Api.Presentation.Web.Auth.Me;

public class MeSummary : ApiSummary<MeEndpoint>
{
    public MeSummary()
    {
        Summary = "Get the current user";
        Description = "Reads the user from the access token cookie. Works for players and guests.";
        Response<MeResponse>(200, "The current user", example: new MeResponse(Guid.Parse("3f2b8c1e-7a4d-4e9b-9c2a-1d5e6f7a8b9c"), "player1", "Player"));
        Response<ApiErrorResponse>(401, "Not logged in (`not_authenticated`)");
    }
}
