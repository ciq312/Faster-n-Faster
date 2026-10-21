using FasterNFaster.Api.Presentation.Web.Shared;

namespace FasterNFaster.Api.Presentation.Web.Auth.GoogleAuth;

public class GoogleCallbackSummary : ApiSummary<GoogleCallbackEndpoint>
{
    public GoogleCallbackSummary()
    {
        Summary = "Finish Google sign-in";
        Description = "Called by Google, not by the client. Logs the player in, sets the auth cookies and redirects to the same-origin `returnUrl`; on failure it redirects with an `error` query parameter.";
        Response(302, "Redirect back to the frontend");
    }
}
