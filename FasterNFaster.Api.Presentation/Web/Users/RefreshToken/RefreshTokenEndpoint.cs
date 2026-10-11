using FastEndpoints;
using FasterNFaster.Api.Presentation.Extensions;
using FasterNFaster.Api.UseCases.Users.RefreshToken;
using FasterNFaster.Api.Presentation.Web.Options.AuthCookies;
using FasterNFaster.Api.Presentation.Web.Services.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Presentation.Web.Users.RefreshToken;

public class RefreshTokenEndpoint(ISender sender, IAuthTokenWriter auth, IOptions<AuthCookiesOptions> options) : EndpointWithoutRequest
{
    private readonly AuthCookiesOptions options = options.Value;

    public override void Configure()
    {
        Post("/api/auth/refresh");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthModerate));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var refreshToken = HttpContext.Request.Cookies[options.RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var result = await sender.Send(new RefreshTokenCommand(refreshToken), ct);

        auth.WriteAuth(result.Tokens);
        await Send.OkAsync(cancellation: ct);
    }
}
