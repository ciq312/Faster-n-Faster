using FasterNFaster.Api.Presentation.Web.Shared;

namespace FasterNFaster.Api.Presentation.Web.Users.ChangeNick;

public class ChangeNickSummary : ApiSummary<ChangeNickEndpoint>
{
    public ChangeNickSummary()
    {
        Summary = "Change the current player's nick";
        Description = "Saves the nick and sets new access and refresh token cookies that carry it. A lobby the player is already in keeps the old nick until they rejoin.";
        ExampleRequest = new ChangeNickRequest("player1");
        Response(200, "Nick changed");
        ValidationFailed();
        Response<ApiErrorResponse>(401, "Not logged in (`not_authenticated`)");
        Response<ApiErrorResponse>(403, "Not a registered player, e.g. a guest (`forbidden`)");
        Response<ApiErrorResponse>(404, "The player no longer exists");
        RateLimited();
    }
}
