using FastEndpoints;
using Microsoft.AspNetCore.Authentication;

namespace FasterNFaster.Api.Web.Users.GoogleAuth;

public class GoogleChallengeEndpoint : Endpoint<GoogleChallengeRequest>
{
    public override void Configure()
    {
        Get("/api/auth/google");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GoogleChallengeRequest req, CancellationToken ct)
    {
        // Google handler will redirect back to our callback after consent. The "returnUrl"
        // is stashed in auth properties so the callback can forward the user to the
        // correct frontend location (validated same-origin there).
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/auth/google/callback",
        };
        properties.Items["returnUrl"] = req.ReturnUrl;

        // Use SendResultAsync so FastEndpoints executes the IResult and marks the
        // response as sent — calling HttpContext.ChallengeAsync directly leaves the
        // response unflushed and FE auto-sends an empty response, wiping the 302.
        await Send.ResultAsync(Results.Challenge(properties, new[] { "Google" }));
    }
}

public class GoogleChallengeRequest
{
    public string? ReturnUrl { get; set; }
}
