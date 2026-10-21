using FasterNFaster.Api.Presentation.Web.Shared;

namespace FasterNFaster.Api.Presentation.Web.Auth.VerifyEmail;

public class VerifyEmailSummary : ApiSummary<VerifyEmailEndpoint>
{
    public VerifyEmailSummary()
    {
        Summary = "Verify an email";
        Description = "Confirms the email with the token from the verification link.";
        ExampleRequest = new VerifyEmailRequest("q8ZtJ3x0nV2mK7pW");
        Response(200, "Email verified");
        Response<ApiErrorResponse>(404, "The token is unknown or expired, or its user no longer exists");
        RateLimited();
    }
}
