using FasterNFaster.Api.Presentation.Web.Shared;

namespace FasterNFaster.Api.Presentation.Web.Auth.GoogleAuth;

public class GoogleChallengeSummary : ApiSummary<GoogleChallengeEndpoint>
{
    public GoogleChallengeSummary()
    {
        Summary = "Start Google sign-in";
        Description = "Redirects the browser to Google's consent screen. Google returns to `/api/auth/google/callback`, which forwards to `returnUrl`.";
        Response(302, "Redirect to Google");
    }
}
