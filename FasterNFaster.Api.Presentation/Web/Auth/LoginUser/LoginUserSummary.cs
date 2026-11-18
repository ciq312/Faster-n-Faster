using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Presentation.Web.Shared;

namespace FasterNFaster.Api.Presentation.Web.Auth.LoginUser;

public class LoginUserSummary : ApiSummary<LoginUserEndpoint>
{
    public LoginUserSummary()
    {
        Summary = "Log in with a login and password";
        Description = "Sets the access and refresh tokens as HTTP-only cookies.";
        ExampleRequest = new LoginUserRequest("player1", "TestPassword123!");
        Response(200, "Logged in", example: new LoginUserResponse(Guid.Parse("3f2b8c1e-7a4d-4e9b-9c2a-1d5e6f7a8b9c"), "player1"));
        Response<ApiErrorResponse>(401, "Wrong login or password");
        Response<ApiErrorResponse>(403, "The email isn't verified; `errors.email` holds the account's email", example: new ApiErrorResponse
        {
            Reason = "Email verification required",
            Code = ErrorCodes.EmailNotVerified,
            Errors = new() { ["email"] = ["player1@example.com"] }
        });
        RateLimited();
    }
}
