using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.UseCases.Auth.RegisterUser;

namespace FasterNFaster.Api.Presentation.Web.Auth.RegisterUser;

public class RegisterUserSummary : ApiSummary<RegisterUserEndpoint>
{
    public RegisterUserSummary()
    {
        Summary = "Register a player account";
        Description = "Creates an unverified account and emails a verification link. The player can log in once the email is verified.";
        ExampleRequest = new RegisterUserRequest("player1", "player1", "player1@example.com", "TestPassword123!");
        Response<RegisterUserResult>(201, "Registered; the verification email is sent", example: new RegisterUserResult(Guid.Parse("3f2b8c1e-7a4d-4e9b-9c2a-1d5e6f7a8b9c"), "player1"));
        ValidationFailed();
        Response<ApiErrorResponse>(409, "The login or the email is already taken");
        RateLimited();
    }
}
