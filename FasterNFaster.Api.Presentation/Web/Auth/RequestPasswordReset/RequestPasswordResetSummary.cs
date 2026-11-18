using FasterNFaster.Api.Presentation.Web.Shared;

namespace FasterNFaster.Api.Presentation.Web.Auth.RequestPasswordReset;

public class RequestPasswordResetSummary : ApiSummary<RequestPasswordResetEndpoint>
{
    public RequestPasswordResetSummary()
    {
        Summary = "Request a password reset";
        Description = "Emails a reset link. Succeeds whether or not the email belongs to an account, so it can't be used to probe for accounts.";
        ExampleRequest = new RequestPasswordResetRequest("player1@example.com");
        Response(200, "Accepted; an email is sent if the account exists");
        ValidationFailed();
        RateLimited();
    }
}
