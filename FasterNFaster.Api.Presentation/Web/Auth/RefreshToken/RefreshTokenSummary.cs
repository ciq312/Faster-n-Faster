using FasterNFaster.Api.Presentation.Web.Shared;

namespace FasterNFaster.Api.Presentation.Web.Auth.RefreshToken;

public class RefreshTokenSummary : ApiSummary<RefreshTokenEndpoint>
{
    public RefreshTokenSummary()
    {
        Summary = "Rotate the auth tokens";
        Description = "Reads the refresh token cookie, revokes it and sets a new access and refresh token pair as cookies.";
        Response(200, "Tokens rotated");
        Response<ApiErrorResponse>(401, "The refresh token is missing, invalid, expired or already rotated (`not_authenticated`)");
        RateLimited();
    }
}
