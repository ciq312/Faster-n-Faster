using FasterNFaster.Api.Presentation.Web.Shared;

namespace FasterNFaster.Api.Presentation.Web.Auth.ResendVerification;

public class ResendVerificationSummary : ApiSummary<ResendVerificationEndpoint>
{
    public ResendVerificationSummary()
    {
        Summary = "Resend the verification email";
        Description = "Succeeds whether or not the email belongs to an unverified account, so it can't be used to probe for accounts.";
        ExampleRequest = new ResendVerificationRequest("player1@example.com");
        Response(200, "Accepted; an email is sent if the account exists and isn't verified");
        RateLimited();
    }
}
