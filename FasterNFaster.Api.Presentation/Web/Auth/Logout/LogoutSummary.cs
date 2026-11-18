using FasterNFaster.Api.Presentation.Web.Shared;

namespace FasterNFaster.Api.Presentation.Web.Auth.Logout;

public class LogoutSummary : ApiSummary<LogoutEndpoint>
{
    public LogoutSummary()
    {
        Summary = "Log out";
        Description = "Revokes the refresh token from the cookie, if any, and clears the auth cookies. Succeeds for anonymous callers too.";
        Response(200, "Logged out");
    }
}
