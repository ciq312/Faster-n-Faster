using FasterNFaster.Api.Presentation.Web.Shared;

namespace FasterNFaster.Api.Presentation.Web.Auth.RegisterAnonymous;

public class RegisterAnonymousSummary : ApiSummary<RegisterAnonymousEndpoint>
{
    public RegisterAnonymousSummary()
    {
        Summary = "Play as a guest";
        Description = "Creates a guest with the given nick and sets its access token as an HTTP-only cookie. Guests get no refresh token.";
        ExampleRequest = new RegisterAnonymousRequest { Nick = "guest1" };
        Response<RegisterAnonymousResponse>(201, "Guest created", example: new RegisterAnonymousResponse(Guid.Parse("8d1c2b3a-4e5f-4a6b-8c7d-9e0f1a2b3c4d")));
        ValidationFailed();
        RateLimited();
    }
}
