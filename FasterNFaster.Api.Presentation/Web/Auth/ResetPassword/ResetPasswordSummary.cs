using FasterNFaster.Api.Presentation.Web.Shared;

namespace FasterNFaster.Api.Presentation.Web.Auth.ResetPassword;

public class ResetPasswordSummary : ApiSummary<ResetPasswordEndpoint>
{
    public ResetPasswordSummary()
    {
        Summary = "Reset the password";
        Description = "Sets a new password with the token from the reset link.";
        ExampleRequest = new ResetPasswordRequest("q8ZtJ3x0nV2mK7pW", "NewPassword123!");
        Response(200, "Password changed");
        ValidationFailed();
        Response<ApiErrorResponse>(404, "The token is unknown or expired, or its user no longer exists");
        RateLimited();
    }
}
