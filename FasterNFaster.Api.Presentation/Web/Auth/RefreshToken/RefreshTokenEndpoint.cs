using FastEndpoints;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Presentation.Extensions;
using FasterNFaster.Api.UseCases.Auth.RefreshToken;
using MediatR;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Presentation.Web.Auth.RefreshToken;

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
            throw new UnauthorizedException();
        }

        var result = await sender.Send(new RefreshTokenCommand(refreshToken), ct);

        auth.WriteAuth(result.Tokens);
        await Send.OkAsync(cancellation: ct);
    }
}
